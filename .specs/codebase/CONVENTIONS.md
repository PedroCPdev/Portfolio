# CONVENTIONS.md

## API
- Minimal API, **sem controllers**. `Program.cs` em 3 fases comentadas com `// ─── FASE N: ... ───`.
- Endpoints agrupados em `Endpoints/` como `static class` + `public static void Map*Endpoints(this WebApplication app)`.
- Uso de `MapGroup` + `.WithTags(...)` por área.
- Construtor primário em classes de serviço (`public class EmailService(IOptions<...> options, ...)`).
- Sem DTOs: os models de `Models/` são serializados direto.
- Respostas de erro do contato usam shape `{ success, error }`; sucesso `{ success, message }`.

## Frontend
- Server components async chamando `src/lib/api.ts` direto (ex.: `Projects.tsx`).
- Falha de fetch é **engolida** e vira estado vazio, nunca erro visível (`getProjects` retorna `[]`)
  — mas só depois de 3 tentativas com backoff, para não confundir cold start com lista vazia.
- Tailwind v4 com classes utilitárias inline sobre **tokens** declarados em
  `src/app/globals.css` (direção "The Decision Record", AD-007): `--ground` `#12140f`,
  `--paper` `#1c1f18`, `--rule` `#2e3327`, `--ink` `#e6e8df`, `--ink-dim` `#9da396`,
  `--amber` `#e0a43c`, `--clay` `#b5563c`. Usar as utilities (`bg-ground`, `text-amber`,
  `border-rule`), nunca hex literal em componente — `design-system.test.ts` falha se um aparecer.
- `--clay` é **reservado** a dois papéis: a linha de custo em `DecisionRecord.tsx` e mensagem de
  erro em `ContactForm.tsx`. Usá-lo em qualquer outro componente quebra o teste que guarda a reserva.
- Largura é responsabilidade de `globals.css`, não de componente: `.shell` é o contêiner da página
  e `.measure` limita a prosa a 65ch. Nenhum componente define `max-w-*` de página — tela larga
  alarga o contêiner, nunca a linha de leitura.
- A home é vitrine e **não** mostra decisão nem custo; o raciocínio vive em `/projects/{id}`.
- Três famílias com papéis fixos: `font-display` (Bricolage Grotesque) só em manchete e título,
  `font-body` (Newsreader) na prosa, `font-mono` (JetBrains Mono) em id, rótulo e tag — nunca prosa.
- As variáveis do `next/font` ficam no `<html>`, não no `<body>`: os tokens são declarados em
  `:root`, e custom property é resolvida onde é **declarada**. Com as fontes no `<body>`,
  `--font-body` referencia um `--font-newsreader` inexistente em `:root`, vira inválida, e a
  prosa cai no sans padrão sem erro de build, lint ou teste (ver L-007).

## Commits
Conventional Commits em português, imperativo minúsculo, um commit por tarefa.
