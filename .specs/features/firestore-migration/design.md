# Design — Migração para Cloud Firestore

## Decisões arquiteturais

**AD-002 — `FirestoreProjectStore` no lugar do `AppDbContext`.**
O projeto não tinha camada de repositório (endpoints chamavam `AppDbContext` direto).
Mantém-se o *espírito* da convenção — uma peça de acesso a dados injetada nos endpoints —
mas ela agora é uma classe própria, porque o `FirestoreDb` não é substituível em teste da
forma que um `DbContext` InMemory seria. A store concentra queries e a normalização de UTC.

**AD-003 — Seed deixa de derrubar o startup.**
Causa raiz do incidente: `MigrateAsync()` + `throw` na FASE 2. Firestore é schemaless, então
a migration some; o seed que resta roda em `try/catch` que **loga e segue**. Um banco fora do ar
passa a degradar só `/api/projects`, sem afetar `/api/contact`.

**AD-004 — Credencial por JSON em env var, não por arquivo.**
Render não tem disco persistente conveniente para secret files. `Firestore:CredentialsJson`
recebe o service account inteiro; `Firestore__CredentialsJson` é a env var equivalente.
Fallback para Application Default Credentials quando vazio (dev local / emulador).

## Contratos de dados / API

Rotas **inalteradas** (`GET /api/projects`, `GET /api/projects/{id}`). Uma quebra de contrato:

| Campo | Antes | Depois | Consequência |
|---|---|---|---|
| `id` | `int` (`1`, `2`) | `string` (`"zcDC9ZKSvTmBCtgkFAZQ"`) | rota `{id:int}` → `{id}`; frontend `id: number` → `string` |

Documento em `projects`: `title`, `description`, `tags` (array), `githubUrl?`, `liveUrl?`,
`imageUrl?`, `createdAt` (timestamp). Nomes em camelCase via `[FirestoreProperty("...")]`,
casando com o JSON que o frontend já consome.

## Análise de reúso

| O que | Reutilizado de | Adaptação |
|---|---|---|
| Padrão `Map*Endpoints(this WebApplication)` | `Endpoints/ProjectsEndpoints.cs:9` | mantido; troca só o tipo injetado |
| Padrão de options + DI | `Services/EmailService.cs:8` (`EmailOptions` + `Configure<T>`) | copiado para `FirestoreOptions` |
| Construtor primário em serviço | `Services/EmailService.cs:20` | aplicado em `FirestoreProjectStore` |
| Seed idempotente | `Services/DataSeeder.cs:11` (`if (await ...AnyAsync()) return;`) | `AnyAsync()` → `Count().GetSnapshotAsync()` |
| try/catch + log na FASE 2 | `Program.cs:52` | mantido, mas **sem** o `throw` (AD-003) |

## Cadeia de Verificação de Conhecimento — pontos verificados

Nenhuma assinatura abaixo foi presumida: todas vieram de reflection sobre o assembly
`Google.Cloud.Firestore 4.4.0` e/ou de execução real contra o emulador.

| Dúvida | Fonte que resolveu | Resposta |
|---|---|---|
| Última versão estável do pacote | NuGet API (flatcontainer) | `4.4.0` |
| Como passar service account JSON sem arquivo | reflection + warning do compilador | `FirestoreDbBuilder.JsonCredentials` **existe mas é `[Obsolete]`** (risco de segurança) |
| Substituto de `JsonCredentials` | warning CS0618 | `GoogleCredential` — mas `GoogleCredential.FromJson` **também é `[Obsolete]`** |
| Substituto de `GoogleCredential.FromJson` | reflection em `CredentialFactory` | `CredentialFactory.FromJson<ServiceAccountCredential>(json)` + `GoogleCredential.FromServiceAccountCredential(...)` → compila com **0 warnings** |
| Atributos de mapeamento | reflection no assembly | `FirestoreData`, `FirestoreProperty`, `FirestoreDocumentId`, `ServerTimestamp` |
| Ordenação decrescente | reflection em `Query` | `OrderByDescending(string)` → `GetSnapshotAsync(ct)` |
| Doc inexistente lança? | execução no emulador | Não — `DocumentSnapshot.Exists == false` |
| `string[]` serializa? | execução no emulador | Sim, round-trip exato (`String[]`, n=2) |
| `DateTime` não-UTC? | execução no emulador | **`ArgumentException`** — exige `DateTimeKind.Utc` (origem de FS-05) |
| `Kind` após leitura | execução no emulador | volta como `Utc` |
| Contagem para idempotência | execução no emulador | `Count().GetSnapshotAsync()` → `.Count` (`long?`) |
| Formato do id gerado | execução no emulador | string de 20 chars (origem de FS-08) |
| Emulador exige qual Java? | log do firebase-tools | JDK **21+** (JDK 25 disponível na máquina) |

## Pontos sinalizados como incertos
Nenhum. Todos os itens acima foram confirmados empiricamente antes da implementação.
