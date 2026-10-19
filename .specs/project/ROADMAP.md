# ROADMAP.md

## Marcos

- [x] API Minimal + seed automático
- [x] Frontend Next.js consumindo /api/projects com ISR 60s
- [x] Formulário de contato via Resend + rate limiting
- [x] Migrar persistência para Cloud Firestore — `features/firestore-migration`
- [x] Mitigar o cold start do Render — `features/render-cold-start`
      (keep-alive 08:00–00:00 BRT + retry no frontend; a madrugada segue com cold start,
      absorvido pelo retry)

- [x] Painel administrativo autenticado — `features/admin-panel`
      (senha + JWT na própria API, CRUD de projetos e editor de decisões em `/admin`;
      revoga a linha de fora de escopo de `PROJECT.md`, ver AD-012)

## Features planejadas

- **Endurecimento de segurança** — backlog da auditoria de 2026-08-18, em
  `codebase/CONCERNS.md` → C-5..C-12, na ordem de correção acordada com o usuário.
  Item 1 (CVEs high do frontend) concluído em `602d02b`; item 2 (C-5, validação de entrada no
  `POST /api/contact`) concluído em 2026-08-26 — `features/contact-input-validation`, 15 testes;
  item 3 (C-6, rate limit por visitante) concluído em 2026-08-26 —
  `features/contact-rate-limit-por-visitante`, 8 testes.
  Item 4 (C-7, container como root) concluído em 2026-10-19 — tarefa rápida `quick/001`.
  Em 2026-10-19 também fecharam C-8, C-9, C-10 (feature `contact-anti-spam`), C-13 e quase todo
  o C-11. **Abertos: `AllowedHosts` do C-11 e C-12, os dois fora do repositório.** C-13 entrou no backlog em 2026-08-24.
  Este ponteiro existe porque `CONCERNS.md` não entra no carregamento base — sem ele,
  o backlog some numa troca de sessão.

Ideias não priorizadas continuam em `STATE.md` → Ideias Adiadas.
