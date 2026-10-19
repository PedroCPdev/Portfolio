# HANDOFF.md

> Ponteiro para retomar rápido, não resumo completo. Sempre sobrescrito — o histórico é o `JOURNAL.md`.

**Data/sessão:** 2026-10-19
**Branch:** `main`, sincronizado com `origin/main`; CI `verify` verde
**Feature ativa:** nenhuma. O backlog de segurança do repositório está fechado.

## Concluído em 2026-10-19

- pedro-harness configurado: `scripts/verify.mjs`, `AGENTS.md`, CI `verify.yml`, Dependabot, `scripts/audit.mjs`.
- API em .NET 10 LTS; container roda como `app` (C-7).
- Keep-alive no cron-job.org (AD-016); o workflow do GitHub ficou só manual.
- next 16.4.2 (3 RCE críticos), vitest 5, Firestore 4.5.0, Scalar 2.18.0, 7 PRs do Dependabot.
- C-8 (headers, AD-017), C-9 (503 sem e-mail), C-10 (honeypot + teto diário), C-11 (quase todo), C-13.

## Pendente, fora do repositório (só o Pedro)

- C-11: env var `AllowedHosts` no Render com o host do serviço (`portfolioapi-p03p.onrender.com`).
- C-12: regras do Firestore (`allow read, write: if false;`), papel da service account
  (Cloud Datastore User) e escopo da chave do Resend.
- Conferir no Render que o deploy usou `sdk:10.0` e roda como `app`.

## Aceito, sem ação

- 5 avisos altos de `braces` em dev via `eslint-config-next`: nenhuma versão corrigida publicada.
- TypeScript preso em 5.x no Dependabot até o `typescript-eslint` suportar o 7.
