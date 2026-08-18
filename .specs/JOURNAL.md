# JOURNAL.md

Histórico append-only de mudanças. Entrada mais recente no topo.
Entradas antigas nunca são reescritas nem apagadas.

---

## 2026-08-18 — Backlog da auditoria de segurança registrado em CONCERNS.md

Os achados da auditoria de segurança deixaram de existir só na conversa: viraram C-5..C-12 em
`codebase/CONCERNS.md`, na ordem de correção acordada, com `arquivo:linha` em cada um. Nenhum
código foi tocado — é registro, não correção.

| Arquivo | Ação | Por quê |
|---|---|---|
| `.specs/codebase/CONCERNS.md` | modificado | C-5..C-12 acrescentados sob um cabeçalho de auditoria, mais a linha de base do que já está correto e não pode regredir |
| `.specs/project/ROADMAP.md` | modificado | ponteiro para o backlog; sem ele o registro não sobreviveria a uma troca de sessão |

**Por que o ponteiro no ROADMAP era necessário:** o carregamento base do harness é
`PROJECT.md` + `ROADMAP.md` + `STATE.md`. `codebase/CONCERNS.md` não entra nele — registrar o
backlog só lá deixaria o arquivo correto e invisível. O ROADMAP é o que faz a próxima sessão
saber que ele existe.

**O que foi registrado além dos problemas:** a seção de auditoria abre com o que a varredura
confirmou estar **certo** (segredos ausentes do histórico, docs de API fechadas em produção,
ausência de endpoints de escrita, React escapando por padrão, `dotnet list --vulnerable` limpo).
Sem isso, uma sessão futura não teria como distinguir "não auditado" de "auditado e aprovado",
e poderia regredir um acerto sem perceber.

**Não toquei:** nenhum arquivo de código, nenhum item do backlog foi corrigido nesta tarefa —
C-5 continua aberto e é o próximo. C-1..C-4 ficaram como estavam; são anteriores e não vieram
desta auditoria, então não entraram na renumeração.

**Gate:** não se aplica — a mudança é exclusivamente documental, sem código, build ou teste
envolvido. `git diff --stat` confirma: só dois arquivos `.specs/`.

**Commit:** `docs(specs): registrar backlog da auditoria de seguranca em concerns`

---

## 2026-08-18 — Next 16.3.1 elimina os 4 CVEs high do bundle de produção

O frontend deixou de subir para produção com dependências vulneráveis: `npm audit --omit=dev`
saiu de 4 vulnerabilidades high para zero. Nenhum comportamento da aplicação mudou — é troca
de versão de dependência, sem alteração de código-fonte.

| Arquivo | Ação | Por quê |
|---|---|---|
| `frontend/package.json:14,25` | modificado | `next` e `eslint-config-next` fixados em 16.3.1; o 16.2.9 arrastava `postcss@8.4.31` e `sharp@0.34.5`, ambos com advisory high |
| `frontend/package-lock.json` | modificado | regenerado por `npm install`: `sharp` 0.34.5 → 0.35.3, `postcss` aninhado do Next 8.4.31 → corrigido, mais 16 pacotes transitivos |
| `.specs/project/STATE.md` | modificado | Ideia Adiada registrando por que `npm audit` (sem `--omit=dev`) ainda mostra 4 achados |

**CVEs eliminados:** `postcss <=8.5.22` (XSS via `</style>` no stringify e três variantes de
path traversal por `sourceMappingURL`) e `sharp <0.35.0` (libvips: CVE-2026-33327, CVE-2026-33328,
CVE-2026-35590, CVE-2026-35591).

**Por que pin explícito e não `npm audit fix --force`:** o `--force` chega ao mesmo 16.3.1, mas
sem deixar no `package.json` o registro de qual versão foi escolhida e por quê. `eslint-config-next`
subiu junto por exigência da própria Next de acompanhar a versão do framework — é devDependency,
não entrava no escopo do CVE.

**Não toquei:** `postcss`, `brace-expansion` e `js-yaml` que seguem vulneráveis via
`@tailwindcss/postcss`, `vitest` e a cadeia do eslint. São devDependencies — não entram no bundle
servido ao visitante, e é por isso que `npm audit --omit=dev` dá zero enquanto `npm audit` dá 4.
Tratá-los agora seria escopo além do item auditado. Registrado como Ideia Adiada. Nenhum arquivo
de código-fonte foi alterado: o diff é só as duas linhas do `package.json` e o lock regenerado.

**Gate (frontend, `TESTING.md`):**
```
npm audit --omit=dev  → found 0 vulnerabilities   (antes: 4 high)
npm test              → 1 arquivo, 5 testes, 5 passed
npm run build         → Next.js 16.3.1, Compiled successfully in 3.2s, 4/4 páginas, exit 0
npm run lint          → exit 0, sem saída
```

**Commit:** `fix(frontend): atualiza next para 16.3.1 e elimina cves de postcss e sharp`

---

## 2026-08-18 — Alinhamento da documentação ao estado real do repositório

A documentação do repositório passou a descrever o que o código faz hoje: Firestore em vez de
Postgres, e-mail via Resend em vez de stub, e o keep-alive registrado como mecanismo permanente
em vez de só como entrada de journal.

| Arquivo | Ação | Por quê |
|---|---|---|
| `README.md` | modificado | descrevia PostgreSQL/EF Core/migrations — nada disso existe desde a migração |
| `CLAUDE.md:56,58,65,77` | modificado | `EmailService` não é stub desde b1d321b; faltavam `/health`, retry e keep-alive |
| `.specs/codebase/INTEGRATIONS.md` | modificado | keep-alive ganhou seção "Como funciona" permanente (gatilho, ordem, sinal de vida, onde desligar) |
| `.specs/codebase/ARCHITECTURE.md` | modificado | fluxo de dados sem `/health`; retry do frontend não aparecia |
| `.specs/codebase/CONCERNS.md` | modificado | C-1 marcado ALTO já mitigado; C-2 dizia "ausência total de testes" |
| `.specs/codebase/STACK.md` | modificado | frontend constava "sem test runner"; existe vitest |
| `.specs/codebase/STRUCTURE.md` | modificado | faltavam `PortfolioApi.Tests/`, `__tests__/`, workflows, `.dockerignore` |
| `.specs/codebase/CONVENTIONS.md` | modificado | a regra do fetch engolido agora menciona o retry antes da desistência |
| `.specs/project/STATE.md` | modificado | B-001 e a seção Pendências removidos a pedido do usuário; L-004..L-006 reordenadas |
| `.specs/project/ROADMAP.md` | modificado | Firestore constava EM ANDAMENTO e cold start como não especificado |
| `.specs/project/PROJECT.md` | modificado | restrição do Render apontava para B-001, que deixou de existir |
| `.specs/features/firestore-migration/tasks.md` | modificado | 22 critérios sem marcar apesar das evidências APROVADO |
| `.specs/features/*/spec.md` | modificado | remover referências órfãs a B-001 |

**Decisões do usuário registradas:** o Supabase só tinha os 2 projetos de exemplo, então a ideia
adiada de migrar dados reais foi encerrada; o segundo problema mencionado na migração já estava
resolvido; a revogação da senha de app do Gmail ficou com o usuário; B-001 foi excluído por
escolha dele — o resíduo do cold start segue documentado em C-1, AD-005 e PROJECT.md.

**Sobre marcar os checkboxes de `tasks.md`:** não houve nova execução de gate. As marcações
citam as evidências já existentes em `evidencias/`, e o cabeçalho do arquivo diz isso
explicitamente para não simular um gate que não rodou nesta tarefa.

**Não toquei:** entradas anteriores do JOURNAL, que citam B-001 — é append-only e reescrever
histórico seria pior que a referência morta. Nenhum arquivo de código foi alterado: o diff é
100% `.md`.

✓ Gate: nenhum, tarefa exclusivamente documental — `git diff --name-only` confirma só `.md`
✓ Commit: `docs: alinhar documentacao ao estado real do repositorio`

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
