# spec — generic-positioning-and-icon

**Escopo:** Médio (6 arquivos, zero decisão arquitetural, zero dependência nova)
**Data:** 2026-08-20

## Problema

Duas coisas, pedidas juntas porque atingem a mesma primeira impressão:

1. A aba do navegador ainda mostra o **favicon padrão do Next.js**, intocado desde o commit
   inicial (`c11f5bf`). O site tem identidade visual própria; o ícone não.
2. O texto do site rotula o autor como engenheiro de **uma stack**. "Backend engineer working in
   C# and .NET" aparece no `<title>`, no trilho do hero e no parágrafo do hero; o About abre com
   "I maintain and build .NET services"; o Contact diz "Open to backend roles and to
   collaborating on .NET work". Um recrutador de Go, Python ou Node lê isso e conclui que a vaga
   dele não é para este candidato — o que é falso, e é o motivo do pedido.

## Decisões do usuário (tomadas antes da implementação)

| # | Pergunta | Resposta |
|---|---|---|
| DU-1 | Qual dos 4 ícones do zip | `icon-brackets.svg` — ⟨P⟩ |
| DU-2 | O que "as descrições" cobre | **Só o texto do site** (arquivos do repo). Nem as `description` dos projetos em produção, nem a lista Core/Also do About |
| DU-3 | Posicionamento | **"Software engineer", sem recorte** — cai também o rótulo "backend" |

**Ressalva registrada e aceita:** o brackets é o que menos aguenta 16px — no preview gerado pelo
próprio usuário os chevrons borram no tamanho de aba. Escolhido mesmo assim.

## Requisitos

| ID | Requisito | Como se observa |
|---|---|---|
| RQ-01 | A aba mostra o ⟨P⟩, não o ícone do Next | `src/app/icon.svg` existe e é o brackets |
| RQ-02 | O favicon padrão do Next some do repositório | `src/app/favicon.ico` não existe |
| RQ-03 | O texto de posicionamento não nomeia stack nem recorte: `C#`, `.NET`, `ASP.NET` e `backend` saem do `<title>`/description, do Hero inteiro, do parágrafo de abertura do About e do Contact | teste lê os 4 arquivos |
| RQ-04 | O autor se apresenta como **software engineer** | `<title>` e description contêm o termo |
| RQ-05 | A tese e o nome do hero sobrevivem | testes DR-03 existentes seguem verdes |
| RQ-06 | Onde `.NET` é **fato**, permanece: tags dos projetos, rodapé da stack do site, lista Core/Also do About | teste afirma que a lista Core ainda traz `.NET` |

## Fora de escopo

- As duas `description` dos projetos em produção (Firestore) — DU-2. Descrevem projetos que
  **são** de fato .NET; genericizá-las seria mentir sobre o que o projeto é.
- Reorganizar a lista Core/Also do About — DU-2. É hoje o sinal .NET mais forte da página
  (4 de 6 do Core), mais forte que o texto, e segue de pé. Registrado como ideia adiada.
- `apple-icon`, `manifest.json`, open-graph image. Não pedidos.
- Versão `.ico` multi-tamanho do brackets para mitigar o borrão a 16px. A doc do Next aceita
  SVG puro; se o borrão incomodar no ar, é uma tarefa própria.

## Rastreabilidade

Gate frontend verde: `npm test` 39/39, `npm run build` compilado, `eslint` exit 0.
Baseline antes da feature era 31 testes — a contagem subiu 8, não caiu.

| ID | Implementação | Teste |
|---|---|---|
| RQ-01 | `frontend/src/app/icon.svg:1` | PS-02 "serve um icon.svg pela convenção de arquivo do app router" (`design-system.test.ts:280`) |
| RQ-02 | `favicon.ico` removido (`git rm`) | PS-02 "o favicon padrão do Next não sobrevive no repositório" (`design-system.test.ts:284`) |
| RQ-03 | `layout.tsx:27-29`, `Hero.tsx:8-9,26-29`, `About.tsx:29-33`, `Contact.tsx:16-17` | PS-01, 4 casos (`design-system.test.ts:244,255,260,267`) |
| RQ-04 | `layout.tsx:27,29` | PS-01 "o metadata se apresenta como software engineer" (`design-system.test.ts:244`) |
| RQ-05 | `Hero.tsx` h1 intocado | DR-03 "o hero traz o nome completo dentro do h1" + "não anuncia o cargo genérico como manchete" (`design-system.test.ts:98,104`) — ambos verdes sem alteração |
| RQ-06 | `About.tsx:11-12` (lista Core intocada) | PS-01 "mantém .NET na lista de tecnologias" (`design-system.test.ts:274`) |

## Evidência observada no artefato construído, não só no teste

`<head>` de `.next/server/app/index.html` traz **exatamente um** link de ícone —
`<link rel="icon" href="/icon.svg?icon.3zccc2hlde9cu.svg" sizes="any" type="image/svg+xml"/>` —
confirmando que o `favicon.ico` não concorre mais. O `<title>` renderizado é
"Pedro Chasci Puga — software engineer" e o texto extraído das quatro seções não contém
`C#`, `.NET` nem `backend`, enquanto a lista Core segue exibindo `C#` e `.NET`.

## Achado durante a execução: custom property não sobrevive fora do navegador

O `icon-brackets.svg` do zip pintava tudo com `var(--bg)` / `var(--fg)` definidos num `<style>`
com `@media (prefers-color-scheme)`. Rasterizado com librsvg 2.62 (o back-end SVG do
ImageMagick, e o mesmo motor de vários crawlers e previews), o ícone saía **quadrado preto
sólido, sem o P** — `fill` e `stroke` viram declaração inválida e são descartados.

Verificado por experimento, não por memória: três SVGs mínimos rasterizados lado a lado —
cor literal ✓, seletor de classe com `@media` ✓, `var(--fg)` ✗. Só a custom property falha;
librsvg suporta CSS de classe.

`src/app/icon.svg` usa seletor de classe em vez de custom property. Geometria idêntica ao
original (mesmos `d`, mesmo `transform`, mesmo `stroke-width`); só a técnica de pintura mudou.
Atributos de apresentação repetem a cor do tema claro como fallback para quem ignorar o
`<style>`. O navegador resolveria as duas formas; a escolhida resolve nos dois mundos.

**Limitação conhecida e aceita pelo usuário:** a 16px os chevrons ficam macios. Confirmado
rasterizando de verdade em 16/32/64 — o P continua identificável, mas o ⟨ ⟩ perde definição.
