# ROADMAP.md

## Marcos

- [x] API Minimal + seed automático
- [x] Frontend Next.js consumindo /api/projects com ISR 60s
- [x] Formulário de contato via Resend + rate limiting
- [x] Migrar persistência para Cloud Firestore — `features/firestore-migration`
- [x] Mitigar o cold start do Render — `features/render-cold-start`
      (keep-alive 08:00–00:00 BRT + retry no frontend; a madrugada segue com cold start,
      absorvido pelo retry)

## Features planejadas

- **Endurecimento de segurança** — backlog da auditoria de 2026-08-18, em
  `codebase/CONCERNS.md` → C-5..C-12, na ordem de correção acordada com o usuário.
  Item 1 (CVEs high do frontend) concluído em `602d02b`; o próximo é C-5.
  Este ponteiro existe porque `CONCERNS.md` não entra no carregamento base — sem ele,
  o backlog some numa troca de sessão.

Ideias não priorizadas continuam em `STATE.md` → Ideias Adiadas.
