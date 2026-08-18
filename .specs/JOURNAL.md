# JOURNAL.md

Histórico append-only de mudanças. Entrada mais recente no topo.
Entradas antigas nunca são reescritas nem apagadas.

---

## 2026-08-18 — Migração concluída e confirmada em produção

Portfólio servindo projetos do Firestore, formulário de contato entregando e-mail, e a API
não depende mais de um banco que pausa.

| Arquivo | Ação | Por quê |
|---|---|---|
| `PortfolioApi/.dockerignore` | criado | impedir que credenciais entrem na imagem em build local |
| `PortfolioApi/appsettings.json` | modificado | alinhar ao que o código lê (não versionado) |
| `.specs/project/STATE.md` | modificado | fechar B-002 e B-003, registrar L-006 |

**Confirmação do usuário:** e-mail chegou, projetos aparecendo. Encerra B-001 na prática
para a janela coberta pelo keep-alive.

**Armadilha registrada como L-006:** o `Microsoft.NET.Sdk.Web` copia qualquer `*.json` do
projeto para a saída do publish. Um `docker build` local assou a service account e o
`appsettings.json` dentro da imagem — produção nunca afetada, pois o Render constrói do
clone do git. O `.gitignore` não cobre isso porque o Docker lê do disco.

**Não toquei:** `Email:ApiKey` local segue vazia — a chave da Resend é do usuário e só
afeta desenvolvimento; produção já está correta.

✓ Produção: `/health` 200 · `/api/projects` 200 com 2 docs · site com cards · keep-alive 200
✓ Commit: `chore(api): impedir que credenciais entrem na imagem docker`

---

## 2026-08-18 — Cold start do Render deixa de esvaziar a seção de projetos

Um cold start da API não faz mais o portfólio aparecer sem projetos; e o keep-alive
mantém a instância acordada na janela de maior tráfego sem estourar a cota do Render.

| Arquivo | Ação | Por quê |
|---|---|---|
| `PortfolioApi/Endpoints/HealthEndpoints.cs` | criado | alvo de ping que responde sem consultar o Firestore |
| `PortfolioApi/Program.cs` | modificado | registrar o endpoint de health |
| `frontend/src/lib/api.ts:17-42` | modificado | insistir durante o religamento em vez de desistir em 8s |
| `frontend/src/lib/__tests__/api.test.ts` | criado | RED: 3 casos de retry falhando antes da implementação |
| `frontend/package.json` | modificado | vitest, para haver como provar lógica de frontend |
| `.github/workflows/keep-api-awake.yml` | criado | manter a instância acordada dentro da cota gratuita |
| `.specs/features/render-cold-start/` | criado | spec, matriz e evidências da feature |

**Por que a janela de 16h e não 24/7:** o Render concede 750 instance-hours/mês e um
serviço dormindo não consome nenhuma. Pingar o tempo todo consumiria ~744 h num mês de
31 dias — margem de 6 h. Estourar a cota faz o Render suspender **todos** os serviços free
até o mês seguinte, o que trocaria uma falha intermitente por uma falha total. Ver AD-005.

**Por que um `/health` em vez de pingar `/api/projects`:** o ping roda ~96 vezes por dia;
usar o endpoint de dados gastaria cota de leitura do Firestore sem ganho e faria o
keep-alive falhar junto com o banco. Ver AD-006.

**Efeito colateral conhecido:** com o retry, uma chamada a `getProjects()` com a API fora
do ar agora leva ~54s antes de desistir, contra 8s antes. Isso atrasa o `next build`
quando a API está indisponível no momento do build.

**Não toquei:** a seção `Email` do `appsettings.json` — está quebrada desde b1d321b (B-003),
mas contém credencial real e não é parte desta tarefa. Também não movi a leitura de
projetos para o Next.js: resolveria o resíduo da madrugada, mas contraria AD-002.

✓ Gate Full: `dotnet build` 0 warnings · API 12/12 · vitest 5/5 · lint e build limpos
✓ Credencial real validada contra o Firestore de produção (somente leitura)
✓ Commit: `feat: absorver cold start do render sem esvaziar a vitrine`

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
