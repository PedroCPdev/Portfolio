# JOURNAL.md

Histórico append-only de mudanças. Entrada mais recente no topo.
Entradas antigas nunca são reescritas nem apagadas.

---

## 2026-08-20 — Tags do Portfolio Frontend em produção (dado real)

Terceira e menor operação em dado real: só o campo `tags` de um documento.

| Alvo | Ação | Por quê |
|---|---|---|
| `projects/R9ZGZNOdNpZtYWkYWnTN` (Portfolio Frontend) | `tags` reescritas | `Next.js` e `React` sem versão não dizem se é App Router ou Pages, nem React 18 ou 19 — e o card da API ao lado já estava versionado |

`TypeScript, Next.js, React, Tailwind CSS` → `TypeScript, Next.js 16, React 19, Tailwind v4, Vercel`.

**Por que versionar.** Numa vitrine de portfólio a versão é metade da informação: "Next.js" pode
ser Pages Router de 2021, "Next.js 16" não. `Vercel` entrou para o card espelhar o da API, que
carrega `Render` — o alvo de deploy é parte do que o projeto demonstra.

**Escrita cirúrgica:** `updateMask.fieldPaths=tags`, e só `tags` no corpo. As 5 decisões, a
`description`, as urls e o `createdAt` seguem intactos, confirmado por comparação com o dump.

**Rollback:** `scratchpad/dump-prod-2026-08-20175940.json`.

**Verificado no site público:** as tags novas apareceram na home ~30s depois, e `Tailwind CSS`
sumiu. Sem redeploy — o ISR de 60s virou sozinho.

**Não toquei:** a `description` do Portfolio Frontend, que segue "Personal Portfolio built using
Next.js 16 and React 19. Minimalistic dark design, C# API integrated." O trecho "C# API
integrated" ficou truncado e agora é redundante com as tags, mas a mudança não foi pedida.


## 2026-08-20 — Mais decisões e tags do Portfolio API em produção (dado real)

Segunda operação em dado real. Os dois projetos foram de 3 para 5 decisões, e as tags do
Portfolio API deixaram de anunciar detalhe interno. Nenhum arquivo de aplicação foi tocado.

| Alvo | Ação | Por quê |
|---|---|---|
| `projects/kBo99k1ss9KSRJCK8HrY` (Portfolio API) | +2 decisões (AD-002, AD-004/L-006) e `tags` reescritas | `NoSQL` e `Scalar` são detalhe interno; `Docker`, `Render` e `Minimal API` são o que o projeto demonstra |
| `projects/R9ZGZNOdNpZtYWkYWnTN` (Portfolio Frontend) | +2 decisões (AD-008, AD-009) | as decisões desta sessão não estavam no site |

**Interpretação assumida.** "as que tomamos" foi lido como *as `AD-NNN` do `STATE.md` que ainda
não estavam publicadas* — inclusive as desta sessão. Produção já tinha 3 decisões em cada projeto
quando o pedido chegou, verificado por leitura antes de escrever, então "está sem decisões" não
podia ser literal.

**Acrescenta, não substitui.** O script lê as decisões existentes e escreve
`[...existentes, ...novas]`, preservando ordem. Um `PATCH` com a lista nova sozinha teria apagado
as três anteriores — o array é substituído inteiro, não mesclado, e essa é a armadilha do
`updateMask` em campo de array.

**Rollback:** `scratchpad/dump-prod-2026-08-20175305.json`, tirado imediatamente antes.

**Verificado no site público, não só no banco:** as 5 decisões e as tags novas aparecem em
`pedrocpdev.vercel.app/projects/kBo99k1ss9KSRJCK8HrY`, cerca de 30s após a escrita — o ISR de 60s
virou sozinho, sem redeploy.

**Não toquei:** as tags do Portfolio Frontend (`TypeScript, Next.js, React, Tailwind CSS`), que não
foram pedidas e não têm o mesmo problema; a `description` do Frontend; e o documento do
"Keep-alive scheduler", que segue inexistente em produção.


## 2026-08-20 — Backfill de `decisions` em produção (dado real)

Operação em dado real, autorizada pelo usuário depois de dump e revisão do conteúdo.
Não é mudança de código: nenhum arquivo da aplicação foi tocado nesta entrada.

| Alvo | Ação | Por quê |
|---|---|---|
| `projects/kBo99k1ss9KSRJCK8HrY` (Portfolio API) | `decisions` (3) + `description` reescrita | a description tinha frase quebrada e virou o resumo visível no card da home |
| `projects/R9ZGZNOdNpZtYWkYWnTN` (Portfolio Frontend) | `decisions` (3) | fecha a página de detalhe com conteúdo real |

**Como foi feito, e por que não pelo console.** À mão seriam 18 entradas de texto dentro de uma
estrutura array-de-map, que o console do Firebase trata mal. Foi um script sem dependências
(`scratchpad/write-prod.mjs`) assinando JWT com a service account de `~/.config/gcloud/`.

**O que protegeu a operação:** `updateMask.fieldPaths` em cada PATCH. Sem ele o Firestore
**substitui o documento inteiro** e `title`, `tags`, `githubUrl`, `liveUrl` e `createdAt` teriam
sido apagados. A verificação pós-escrita comparou todos os campos contra o dump: preservados.

**Rollback disponível:** `scratchpad/dump-prod-2026-08-20174041.json` guarda o estado anterior dos
dois documentos. Mesmo sem rollback, apagar `decisions` devolve array vazio na leitura e a página
degrada sem quebrar — é o que o teste PD-02 garante.

**Não toquei:** a `description` do Portfolio Frontend (não foi aprovada; segue "Personal Portfolio
built using Next.js 16 and React 19. Minimalistic dark design, C# API integrated."). Não criei o
documento do "Keep-alive scheduler" — ele nunca existiu em produção, era invenção minha para o
preview local. Nenhuma regra, índice ou outra coleção foi tocada.

**Ordem importa e foi respeitada:** o backfill entrou **antes** do deploy. A API que está no ar
ainda não conhece `decisions` e simplesmente ignora o campo, então nada quebrou no intervalo — e
quando o deploy entrar, as páginas de detalhe já nascem com conteúdo.


## 2026-08-20 — Nome completo no cabeçalho do hero

O hero passou a abrir com "Pedro Chasci Puga" acima da tese. É página de portfólio: o nome de
quem assina precisava estar visível, não só no `<title>` e no handle da navbar.

| Arquivo | Ação | Por quê |
|---|---|---|
| `frontend/src/components/Hero.tsx` | modificado | nome como primeira linha **dentro** do `<h1>`, em display face menor que a tese |
| `frontend/src/__tests__/design-system.test.ts` | modificado | asserção de que o nome completo está dentro do `h1`, não solto acima dele |

**Por que dentro do `h1` e não num elemento próprio:** separar o nome em um `<p>` acima o tiraria
da estrutura de títulos da página. Numa página de portfólio o nome é o cabeçalho principal —
junto com a tese ele forma o `h1`, o que também é o que a busca por "Pedro Chasci Puga" indexa.
O peso visual continua na tese: o nome é bem menor e sem o extrabold.

**Não toquei:** o handle `pedrocpdev` na navbar e no footer continua como está — é o identificador
curto, e repetir o nome completo nos três lugares seria redundante.

**Gate:** `npx vitest run` → 31 passed · `npx eslint .` → exit 0 · `npm run build` → compilou.


## 2026-08-20 — Vitrine na home, raciocínio em `/projects/{id}`, e largura que acompanha a tela

A home voltou a ser vitrine: um card por projeto, em grid que vai de 1 a 3 colunas. O raciocínio
de engenharia saiu dela e ganhou página própria, onde cada decisão aparece com o porquê e o custo.
O campo `tradeoff` (string única) deu lugar a `decisions: Decision[]`.

| Arquivo | Ação | Por quê |
|---|---|---|
| `PortfolioApi/Models/Decision.cs` | criado | `title`/`why`/`cost`; a ordem na lista é informação, não detalhe de armazenamento |
| `PortfolioApi/Models/Project.cs` | modificado | `Decisions` (`:31`) no lugar de `Tradeoff`; default `[]`, nunca null |
| `PortfolioApi/Services/DataSeeder.cs` | modificado | dois projetos de exemplo com duas decisões cada |
| `PortfolioApi.Tests/FirestoreProjectStoreTests.cs` | modificado | round-trip da lista aninhada, ordem, custo vazio, e documento cru sem a chave |
| `PortfolioApi.Tests/ProjectContractTests.cs` | modificado | `decisions[].title/why/cost` em camelCase, array vazio, e ausência de `tradeoff` |
| `frontend/src/lib/api.ts` | modificado | `Decision`, `decisions?`, e `getProject` (`:65`); retry de cold start extraído para servir as duas chamadas |
| `frontend/src/app/projects/[id]/page.tsx` | criado | rota de detalhe, `generateStaticParams`, `generateMetadata`, `notFound()` |
| `frontend/src/components/DecisionRecord.tsx` | renomeado de `Ledger.tsx` | um registro por decisão; `why` e `cost` lado a lado a partir de `lg` |
| `frontend/src/components/Projects.tsx` | modificado | grid de cards `1 → 2 → 3` colunas, cada card linkando para o detalhe |
| `frontend/src/components/Hero.tsx` | modificado | bloco AD-001 removido: decisão não mora mais na home |
| `frontend/src/components/Section.tsx` | modificado | usa `.shell`; título com `clamp` |
| `frontend/src/components/Navbar.tsx` | modificado | `<Link>` no lugar de `<a>` (a nav existe em duas rotas agora), âncoras absolutas `/#work` |
| `frontend/src/app/globals.css` | modificado | `.shell` (`:61`) e `.measure` (`:82`) |
| `frontend/src/app/page.tsx`, `About.tsx`, `Contact.tsx` | modificados | contêiner e medida de leitura compartilhados |
| `frontend/src/__tests__/design-system.test.ts` | modificado | bloco DR-05 substituído pelos PD-01/03/04/05/08 |
| `.specs/features/project-detail-page/` | criado | spec PD-01..PD-09 com matriz |
| `.specs/project/STATE.md` | modificado | AD-008 (separar vitrine de raciocínio) e AD-009 (largura em `.shell`/`.measure`) |
| `.specs/codebase/CONVENTIONS.md`, `TESTING.md`, `CLAUDE.md` | modificados | reserva do clay aponta para o arquivo novo, regra de largura, contagem do gate, e as duas rotas |

**Por que os testes de `tradeoff` sumiram.** Não foram apagados para reduzir falha: o campo deixou
de existir por decisão do usuário (AD-008), e cada um foi substituído pelo equivalente em
`decisions` — inclusive o mais importante, o que grava um documento **cru** sem a chave para provar
que documento antigo não estoura na leitura. A contagem subiu: 18 → 20 na API, 22 → 30 no frontend.

**A queixa de "tudo centralizado" era estrutural.** O layout prendia tudo em `max-w-5xl` (64rem)
repetido em oito arquivos. A correção não foi aumentar o número: foi separar largura de contêiner
(`.shell`) de largura de leitura (`.measure`), porque esticar a segunda junto com a primeira só
troca uma coluna estreita por linha de texto ilegível. Um teste falha se `max-w-5xl` voltar.

**Duas correções vieram do screenshot, não do gate:** as réguas de `Why`/`Cost` esticavam pela
coluna inteira enquanto o texto parava em 65ch, deixando régua pendurada sobre vazio; e o registro
de decisão tinha três níveis de indentação. Ambas invisíveis para build, lint e testes.

**Não toquei:** os documentos de produção seguem sem `decisions`, então em `pedrocpdev.vercel.app`
cada página de detalhe vai dizer que as decisões ainda não foram escritas até você preenchê-las.
Não criei endpoint de escrita nem painel admin. Não mexi em `imageUrl`, que continua no model sem
uso na UI.

**Gate:** `dotnet test PortfolioApi.Tests` → 20 passed (emulador) · `npx vitest run` → 30 passed ·
`npx eslint .` → exit 0 · `npm run build` → 7 rotas, 3 páginas de projeto pré-renderizadas.


## 2026-08-20 — Campo `tradeoff` no contrato: API, Firestore e frontend

O trade-off ledger passou a ter as duas metades. `Project.Tradeoff` existe no model, é gravado e
lido do Firestore, sai como `tradeoff` no JSON de `GET /api/projects`, e o frontend deixou de
estender o tipo localmente. O seed carrega um custo real em cada projeto de exemplo.

| Arquivo | Ação | Por quê |
|---|---|---|
| `PortfolioApi/Models/Project.cs` | modificado | `Tradeoff` opcional (`:30`), posicionado junto de `Description` porque são as duas metades do mesmo par |
| `PortfolioApi/Services/DataSeeder.cs` | modificado | custo real nos dois projetos semeados — é a única fonte de exemplo do ledger para quem sobe do zero |
| `PortfolioApi.Tests/FirestoreProjectStoreTests.cs` | modificado | round-trip, ausência, documento legado sem a chave, e seed com custo |
| `PortfolioApi.Tests/ProjectContractTests.cs` | criado | trava o nome `tradeoff` no JSON; sem DTO (C-4), renomear a propriedade muda o contrato calado |
| `frontend/src/lib/api.ts` | modificado | `tradeoff?: string` no tipo `Project` |
| `frontend/src/components/Projects.tsx` | modificado | alias `ProjectRecord` removido: o campo agora vem do contrato de verdade |
| `frontend/src/__tests__/design-system.test.ts` | modificado | asserção TR-05: o alias local não pode voltar |
| `.specs/features/project-tradeoff-field/` | criado | spec TR-01..TR-05 com matriz e o risco coberto pelo TR-02 |
| `.specs/codebase/TESTING.md` | modificado | contagem do gate `full` estava em 17 e agora são 40; seção nova sobre os testes que rodam sem emulador |
| `CLAUDE.md` | modificado | `Tradeoff` é opcional por construção — documento antigo sem a chave lê como null |

**Sem migration, e o risco não é esse.** O Firestore é schemaless: o campo novo não exige DDL nem
backfill, e nenhum dado de produção foi tocado. O risco real é o inverso — os documentos que já
estão lá **não têm** a chave `tradeoff`, e se `ConvertTo<Project>()` estourasse com ela ausente,
`GET /api/projects` quebraria inteiro no primeiro deploy. Um teste que grava e lê pelo próprio
model nunca pegaria isso, porque o model sempre grava a chave. Por isso
`FirestoreProjectStoreTests.cs:176` escreve um documento **cru**, via dicionário, sem a chave, e
prova que a leitura devolve null sem lançar.

**Verificado ponta a ponta, não só em unitário.** Com o emulador no ar, a API real subiu contra
ele, o seed rodou, `GET /api/projects` devolveu `"tradeoff"` em camelCase nos dois documentos, e o
frontend renderizou as duas linhas de custo. A coleção usada no e2e foi `projects_e2e_tradeoff`,
no emulador — produção intacta.

**Não toquei:** os documentos de produção continuam sem `tradeoff`, então em
`pedrocpdev.vercel.app` o ledger segue mostrando só a linha "Kept" até você preencher o campo no
console do Firebase. Isso é escrita em dado real e é decisão sua, não minha. Não criei endpoint de
escrita nem painel admin (fora de escopo no nível projeto). Não tornei o campo obrigatório.

**Gate:** `dotnet test PortfolioApi.Tests` → 18 passed (com emulador) · `npx vitest run` → 22 passed ·
`npx eslint .` → exit 0 · `npm run build` → compiled successfully.


## 2026-08-20 — Redesign visual do frontend: "The Decision Record"

O portfólio deixou de parecer template de dev. A página passou a ter a forma do artefato que
melhor representa o Pedro como engenheiro — o registro de decisão com o custo declarado — com
paleta em tokens (oliva-grafite + âmbar + clay), três famílias tipográficas com papéis fixos,
spine assimétrico e o *trade-off ledger* como elemento-assinatura. Nenhuma linha de
`src/lib/api.ts`, do contrato de `/api/projects` ou da API .NET foi tocada.

| Arquivo | Ação | Por quê |
|---|---|---|
| `frontend/src/app/globals.css` | modificado | os 7 tokens de cor, os 3 stacks de fonte, `:focus-visible:60` e o bloco `prefers-reduced-motion:73` — a fundação de que todo componente depende |
| `frontend/src/app/layout.tsx` | modificado | troca Outfit por Bricolage + Newsreader + JetBrains; variáveis do `next/font` no `<html>` (L-007) |
| `frontend/src/app/page.tsx` | modificado | container sai do wrapper e vai para cada seção, para as bordas irem de ponta a ponta; footer reescrito |
| `frontend/src/components/Section.tsx` | criado | primitiva do spine: dono único do grid e do espaçamento, para as seções não definirem padding próprio e brigarem entre si |
| `frontend/src/components/Ledger.tsx` | criado | o elemento-assinatura; extraído porque Hero e Projects usam o mesmo par decisão/custo |
| `frontend/src/components/SectionHeader.tsx` | removido | existia para renderizar o ordinal `01/02/03` de seções que não são sequência |
| `frontend/src/components/Hero.tsx` | modificado | tese no lugar do cargo genérico, e o AD-001 real logo abaixo como prova imediata |
| `frontend/src/components/About.tsx` | modificado | nuvem de 19 tags vira Core (6, com peso) + Also worked with (5, discreto) |
| `frontend/src/components/Projects.tsx` | modificado | cards viram registros com ledger; `tradeoff` estendido localmente para não tocar em `api.ts`; estado vazio vira convite |
| `frontend/src/components/Contact.tsx` | modificado | copy e links no vocabulário novo; Instagram removido do conjunto profissional |
| `frontend/src/components/ContactForm.tsx` | modificado | labels reais em vez de placeholder como rótulo, `aria-live` no status, e o verbo sobrevive até a confirmação |
| `frontend/src/components/Navbar.tsx` | modificado | sentence case e tokens; `Projects` vira `Work` para casar com a seção |
| `frontend/src/__tests__/design-system.test.ts` | criado | 16 invariantes mecânicas do sistema de design |
| `.specs/features/visual-decision-record/` | criado | spec (DR-01..DR-11), design e tarefas da feature |
| `.specs/codebase/CONVENTIONS.md` | modificado | a regra antiga proibia literalmente o que o AD-007 decidiu fazer |
| `.specs/project/STATE.md` | modificado | AD-007 (a decisão e o que ela custou), L-007 e uma ideia adiada |
| `CLAUDE.md` | modificado | descrevia a paleta dark-navy que deixou de existir |

**O gate não pega design, e quase escondeu um bug.** Com as variáveis do `next/font` no
`<body>`, `--font-body` (declarado em `:root`) referenciava um `--font-newsreader` que não existe
naquele escopo. O token virava inválido, o `font-family` do body era descartado, e toda a prosa
caía no sans padrão — com **build, lint e 21 testes verdes**. Só apareceu ao olhar o pixel. Está
em L-007, e `design-system.test.ts:60` guarda a correção.

**Reserva do clay.** `--clay` só pode aparecer na linha de custo do ledger e em mensagem de erro.
Não é convenção escrita torcendo para ser seguida: `design-system.test.ts:156` falha se qualquer
outro componente usar. É o que impede o acento de virar decoração e a assinatura de se diluir.

**Evidência, sem inflar:** 21/21 testes, lint 0, build ok. Mas DR-04 (spine) e DR-11 (hierarquia
de tecnologias) são composição visual e o gate **não** os cobre — não há jsdom no projeto.
Estão marcados na matriz como *verificado por inspeção*, com screenshot em 1440px e 390px.

**Não toquei:** `src/lib/api.ts`, o contrato de `/api/projects` e a API .NET inteira — o campo
`tradeoff` é opcional e estendido no componente, então nenhum projeto existente quebra e o
ledger simplesmente não renderiza o bloco de custo quando o campo não vem. Não adicionei
`@testing-library`/`jsdom` (fora de escopo). Não corrigi `CLAUDE.md:23` ("No test runner is
configured"), que é falso desde que o vitest entrou — está registrado em Ideias Adiadas.
`frontend/AGENTS.md` e `frontend/CLAUDE.md`, gerados pelo `next dev`, ficaram fora do commit.

**Gate:** `npx vitest run` → 21 passed (2 files) · `npx eslint .` → exit 0 ·
`npm run build` → compiled successfully, 4/4 páginas estáticas.


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
