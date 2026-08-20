# Spec: visual-decision-record

Escopo classificado como: **Grande** (multi-componente — 8 arquivos, toda a superfície visível do site).

Redesign visual completo do frontend sob a direção **"The Decision Record"**: a página deixa de
ser um portfólio-template genérico e passa a ter a forma do artefato que melhor representa o
Pedro como engenheiro — o registro de decisão com trade-off explícito, como os `AD-NNN` de
`.specs/project/STATE.md`.

Sem mudança de contrato de API, de `src/lib/api.ts` ou de lógica de dados. Só apresentação e copy.

## Requisitos

### P1 — MVP (fatia vertical, demonstrável ponta a ponta)

**[DR-01] Sistema de tokens**
QUANDO qualquer componente precisar de cor ou fonte
ENTÃO o sistema DEVE resolvê-la a partir de um token declarado em `globals.css`
(`--ground`, `--paper`, `--rule`, `--ink`, `--amber`, `--clay`), e NÃO DEVE conter
nenhum hex da paleta antiga (`#050d1a`, `#0d1b2e`, `#5ba0f5`, `#e8f0fe`) em `src/`.

**[DR-02] Pareamento tipográfico**
QUANDO a página carregar
ENTÃO o sistema DEVE servir três famílias com papéis distintos — Bricolage Grotesque
(display), Newsreader (corpo) e JetBrains Mono (utilitário) — cada uma exposta como
CSS variable pelo `next/font/google`, sem `<link>` externo.

**[DR-03] Hero como tese**
QUANDO o visitante abrir a página
ENTÃO o hero DEVE apresentar uma afirmação específica sobre o trabalho do Pedro
(não o rótulo genérico "Software Developer") e NÃO DEVE conter o eyebrow `// hello world`.

**[DR-04] Spine assimétrico**
QUANDO uma seção for renderizada em viewport ≥1024px
ENTÃO ela DEVE usar o grid de duas colunas (trilho de metadados em mono à esquerda,
conteúdo à direita) e DEVE colapsar para uma coluna abaixo desse breakpoint.

**[DR-05] Trade-off ledger (elemento-assinatura)**
QUANDO um projeto tiver `tradeoff` preenchido
ENTÃO o card DEVE exibir o par decisão/custo com o custo marcado em `--clay`,
e QUANDO não tiver, o card DEVE renderizar sem o bloco de ledger e sem espaço morto.

**[DR-06] Numeração honesta**
QUANDO uma seção não representar uma sequência real
ENTÃO ela NÃO DEVE exibir marcador ordinal (`01`/`02`/`03`);
marcador ordinal fica restrito a conteúdo que é de fato uma sequência.

**[DR-07] Foco visível**
QUANDO o visitante navegar por teclado
ENTÃO todo elemento interativo (link, botão, campo) DEVE exibir um anel de foco
com contraste próprio contra `--ground`.

**[DR-08] Movimento reduzido**
QUANDO o sistema operacional sinalizar `prefers-reduced-motion: reduce`
ENTÃO o sistema DEVE neutralizar animação e transição (duração ≤0.01ms).

**[DR-09] Vocabulário consistente**
QUANDO uma ação for rotulada com um verbo
ENTÃO a confirmação correspondente DEVE usar o mesmo verbo
(`Send message` → `Message sent`), em sentence case, sem rótulo minúsculo decorativo.

**[DR-10] Gate verde**
QUANDO o gate do frontend rodar
ENTÃO `npm test`, `npm run build` e `npm run lint` DEVEM passar sem erro.

### P2 — incrementos além do MVP

**[DR-11] Hierarquia de tecnologias**
QUANDO a seção About listar tecnologias
ENTÃO ela DEVE separar o núcleo defensável do restante, em vez de uma nuvem plana
de 19 itens com peso visual idêntico.

## Fora de escopo

- Qualquer mudança em `src/lib/api.ts`, no contrato `GET /api/projects` ou na API .NET.
  O campo `tradeoff` do [DR-05] é **opcional** e lido do payload existente se vier; não há
  trabalho de backend nesta feature.
- Adicionar `@testing-library/react` / `jsdom`. O gate cobre invariantes de origem
  (tokens, ausência de hex legado, foco, reduced-motion), não renderização em DOM.
- Modo claro / theme switcher.
- Internacionalização. O site segue em inglês.
- Página de projeto individual, blog, ou qualquer rota nova.
- Substituir `react-icons` por `lucide-react` (já instalado, mas trocar é churn fora do escopo).

## Matriz de rastreabilidade

| ID | Tarefa | Implementação | Teste | Evidência | Status |
|---|---|---|---|---|---|
| DR-01 | T1 | `app/globals.css:9` | `design-system.test.ts:27,34` | gate 21/21 | Verificado |
| DR-02 | T1 | `app/layout.tsx:2,7` | `design-system.test.ts:49,60,74` | gate 21/21 | Verificado |
| DR-03 | T3 | `components/Hero.tsx:17` | `design-system.test.ts:83,98` | gate + screenshot | Verificado |
| DR-04 | T2 | `components/Section.tsx:16` | — | screenshot 1440px e 390px | Verificado por inspeção |
| DR-05 | T5 | `components/Ledger.tsx:20`, `Projects.tsx:33` | `design-system.test.ts:139,145,151,156` | gate 21/21 | Verificado |
| DR-06 | T2/T5 | `components/Projects.tsx:14` | `design-system.test.ts:89` | gate 21/21 | Verificado |
| DR-07 | T1 | `app/globals.css:60` | `design-system.test.ts:105` | gate 21/21 | Verificado |
| DR-08 | T1 | `app/globals.css:73` | `design-system.test.ts:114` | gate 21/21 | Verificado |
| DR-09 | T6 | `components/ContactForm.tsx:88` | `design-system.test.ts:124,130` | gate 21/21 | Verificado |
| DR-10 | T8 | — | `npm test` + `build` + `lint` | 21 testes, lint 0, build ok | Verificado |
| DR-11 | T4 | `components/About.tsx:7` | — | screenshot (Core vs Also worked with) | Verificado por inspeção |

Status possíveis: Não iniciado · Em andamento · Verificado · Não atendido

**Nota de honestidade sobre a evidência.** DR-04 e DR-11 são requisitos de composição visual:
não há jsdom no projeto e o gate não os cobre. Estão marcados como *verificado por inspeção*,
com screenshot, e não como verificados por teste. Ler "21/21 verde" como "o design está bom"
seria exatamente o erro que o harness existe para impedir — o gate prova invariante mecânica
(token, ausência de hex legado, foco, movimento reduzido, reserva do clay), não juízo estético.

**L-007 nasceu aqui:** com as fontes no `<body>`, a prosa caía no sans padrão e **o gate
continuava verde**. Foi pego olhando o pixel. O teste `design-system.test.ts:60` existe para
que não volte.
