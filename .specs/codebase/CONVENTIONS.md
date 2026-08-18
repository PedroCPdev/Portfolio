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
- Falha de fetch é **engolida** e vira estado vazio, nunca erro visível (`getProjects` retorna `[]`).
- Tailwind v4 com classes utilitárias inline e paleta dark-navy **hex literal**:
  fundo `#050d1a`, card `#0d1b2e`, acento `#5ba0f5`, texto `#e8f0fe` (com opacidade `/40`, `/25`...).
  **Não introduzir cores novas nem tokens temáticos** — seguir os hex existentes.

## Commits
Conventional Commits em português, imperativo minúsculo, um commit por tarefa.
