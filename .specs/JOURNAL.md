# JOURNAL.md

Histórico append-only de mudanças. Entrada mais recente no topo.
Entradas antigas nunca são reescritas nem apagadas.

---

## 2026-08-18 — Proteger service account do Firebase contra commit acidental

Chave privada real adicionada ao repositório pelo usuário passou a ser ignorada pelo git,
por padrão amplo em vez de caminho único.

| Arquivo | Ação | Por quê |
|---|---|---|
| `.gitignore:8-14` | modificado | qualquer service account do Firebase/GCP passa a ser ignorada, não só este arquivo |
| `.specs/JOURNAL.md` | criado | primeiro registro do log append-only de mudanças |
| `.specs/project/STATE.md` | modificado | registrar auditoria do histórico (B-002) e config local defasada (B-003) |

**Por que padrão amplo e não o nome do arquivo:** ignorar
`PortfolioApi/portfoliodb-9215c-firebase-adminsdk-fbsvc-d864c5e256.json` protegeria só esta
chave. Gerar uma nova (nome diferente) ou salvá-la em outra pasta voltaria a expor. Os padrões
`*firebase-adminsdk*.json`, `*serviceAccount*.json`, `*service-account*.json` e
`gcp-credentials*.json` cobrem o formato, não a instância.

**Auditoria feita antes de qualquer coisa:** a chave estava como untracked e o histórico
completo (`git rev-list --all`) não contém `BEGIN PRIVATE KEY` nem `private_key` em nenhum
commit. **Não houve vazamento; rotação não é necessária.**

**Não toquei:** `PortfolioApi/appsettings.json` — contém secrets reais do usuário e está
defasado (ver B-003); alterar config com credencial real exige confirmação dele.
Também não movi a chave para fora do repositório: é decisão do usuário.

✓ Verificação: `git check-ignore -v` confirma a regra ativa; a chave sumiu do `git status`.
✗ Commit: não commitado — usuário ainda não autorizou e o branch é `main`.

---

## 2026-08-18 — Migrar persistência de PostgreSQL/Supabase para Cloud Firestore

`GET /api/projects` e `/api/projects/{id}` passam a ler do Firestore, e o banco indisponível
deixa de derrubar a API inteira no startup.

| Arquivo | Ação | Por quê |
|---|---|---|
| `PortfolioApi/PortfolioApi.csproj` | modificado | trocar EF Core + Npgsql por `Google.Cloud.Firestore` 4.4.0 |
| `PortfolioApi/Program.cs` | modificado | compor `FirestoreDb` + store; seed que loga e segue em vez de derrubar |
| `PortfolioApi/Models/Project.cs` | modificado | mapear para documento; `Id` string; normalizar `DateTime` para UTC |
| `PortfolioApi/Data/FirestoreProjectStore.cs` | criado | concentrar todo acesso ao Firestore num ponto testável |
| `PortfolioApi/Data/FirestoreOptions.cs` | criado | credencial por env var, sem arquivo em disco no Render |
| `PortfolioApi/Data/AppDbContext.cs` | removido | sem EF Core não há contexto |
| `PortfolioApi/Migrations/` (3 arquivos) | removido | Firestore é schemaless |
| `PortfolioApi/Endpoints/ProjectsEndpoints.cs` | modificado | injetar a store; rota `{id}` sem `:int` |
| `PortfolioApi/Services/DataSeeder.cs` | modificado | idempotência por contagem da coleção |
| `PortfolioApi/appsettings.Example.json` | modificado | exemplo da seção `Firestore` no lugar de `ConnectionStrings` |
| `PortfolioApi.Tests/` (3 arquivos) | criado | primeiro projeto de testes do repo; 12 casos contra emulador |
| `frontend/src/lib/api.ts:4` | modificado | id do Firestore é string, não number |
| `CLAUDE.md` | modificado | refletir a nova stack e o comando de testes |
| `.specs/` | criado | mapeamento do codebase + spec/design/tasks/evidências |

**Por que uma store nova:** o projeto não tinha camada de repositório (endpoints chamavam
`AppDbContext` direto). `FirestoreDb` não é substituível em teste como um `DbContext`
InMemory seria, então a store preserva o espírito da convenção — uma peça de dados injetada —
e viabiliza os testes. Ver AD-002.

**Efeito colateral conhecido:** `id` deixou de ser `int` e virou `string` — quebra de contrato
público da API, propagada ao frontend.

**Não toquei:** `EmailService`, `ContactEndpoints` e componentes visuais — fora do escopo.
Cold start do Render segue aberto como B-001; a migração não o resolve.

✓ Gate Full: `dotnet build` 0 warnings · `dotnet test` 12/12 · `npm run lint` + `build` limpos
✓ E2E HTTP real: FS-01 a FS-07 verificados (`.specs/features/firestore-migration/evidencias/`)
✗ Commit: não commitado — usuário ainda não autorizou.
