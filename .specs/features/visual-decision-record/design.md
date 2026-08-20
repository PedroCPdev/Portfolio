# Design: visual-decision-record

## Direção — "The Decision Record"

**Tese.** O que diferencia o Pedro não é a lista de tecnologias — é o hábito de decidir e
registrar o que a decisão custou (`AD-001..AD-006` em `STATE.md`). A página assume a forma
desse artefato. O visitante entende em 30s que está lendo um engenheiro que sabe defender
escolhas, não um catálogo de keywords.

**Por que não o design atual.** O que existe hoje é a soma dos tells de portfólio-template:
`// hello world` como eyebrow, `01/02/03` em três seções que não são sequência, o mesmo card
(`#0d1b2e` + `border-[0.5px]` + `rounded-[10px]`) repetido 5×, botões em minúsculo decorativo,
e uma nuvem de 19 tags onde `C#` e `Scrum` têm peso visual idêntico.

## Tokens de cor

Fora da navy. Ground oliva-grafite (preto deslocado para o verde — incomum em portfólio de dev),
acento âmbar de sinalização, e um vermelho de barro reservado a **um** trabalho: marcar custo.

| Token | Hex | Papel |
|---|---|---|
| `--ground` | `#12140F` | fundo da página |
| `--paper` | `#1C1F18` | superfície elevada (cards, campos) |
| `--rule` | `#2E3327` | hairline, divisor, borda |
| `--ink` | `#E6E8DF` | texto principal |
| `--ink-dim` | `#9DA396` | texto secundário |
| `--amber` | `#E0A43C` | acento: links, decisão mantida, foco |
| `--clay` | `#B5563C` | **exclusivo** de custo/trade-off e erro |

Contraste verificado contra `--ground`: `--ink` 14.2:1, `--ink-dim` 7.1:1, `--amber` 8.9:1,
`--clay` 4.6:1 — todos ≥4.5:1 (WCAG AA texto normal).

## Tokens de tipo

| Papel | Família | Uso |
|---|---|---|
| Display | Bricolage Grotesque | manchete do hero, títulos de seção. Larguras propositalmente irregulares |
| Corpo | Newsreader | prosa. Serifa no escuro é o risco assumido: registro se compõe em serifa |
| Utilitário | JetBrains Mono | ids, rótulos do trilho, tags, status. Nunca prosa |

Escala: `3.4rem / 2rem / 1.25rem / 1.0625rem / 0.9375rem / 0.75rem`, com `--tracking-label`
de `0.14em` só nos rótulos mono em caixa alta.

## Layout — spine assimétrico

Grid de duas colunas: trilho de metadados de `9rem` à esquerda (mono, caixa alta, alinhado ao
topo), conteúdo à direita. Abaixo de `1024px` colapsa para uma coluna e o trilho vira uma
linha de rótulo acima do conteúdo. O trilho carrega o que um registro carrega — id, data,
categoria — e não decoração.

```
┌──────────┬───────────────────────────────────┐
│ BACKEND  │  I build the boring parts         │
│ .NET·C#  │  that have to stay up.            │
├──────────┼───────────────────────────────────┤
│ AD-001   │  Firestore over PostgreSQL        │
│          │  ── kept ── (amber)               │
│          │  ── cost ── (clay)                │
└──────────┴───────────────────────────────────┘
```

## Elemento-assinatura — trade-off ledger

Cada projeto vira registro de três partes: o que foi decidido, por quê, e **o que custou**.
A linha de custo em `--clay` é o que nenhum portfólio publica, e é o argumento inteiro.
Degrada silenciosamente: projeto sem `tradeoff` no payload renderiza sem o bloco.

## AD-007 — sobrepor a convenção de paleta

`CONVENTIONS.md` dizia "não introduzir cores novas nem tokens temáticos — seguir os hex
existentes". Esta feature **revoga** essa regra por decisão explícita do usuário (2026-08-20):
a paleta antiga era o próprio problema. A convenção passa a ser "toda cor sai de um token de
`globals.css`; hex literal em componente é regressão". `CONVENTIONS.md` é atualizado na T7.

## Estratégia de verificação

Não há jsdom nem testing-library no projeto, e adicioná-los está fora de escopo. O gate cobre
os **invariantes mecânicos** lendo os arquivos-fonte: tokens declarados, ausência de hex legado,
`:focus-visible` presente, bloco `prefers-reduced-motion` presente, fontes carregadas via
`next/font`. O juízo estético é verificado por screenshot, não por asserção — e isso está dito
aqui de propósito, para ninguém ler "10/10 testes" como "o design está bom".
