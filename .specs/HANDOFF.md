# Transição

**Data:** 2026-10-19
**Feature:** nenhuma ativa
**Tarefa:** backlog de segurança do repositório concluído

## Concluído ✓

- pedro-harness: `scripts/verify.mjs`, `AGENTS.md`, CI `verify.yml`, Dependabot, `scripts/audit.mjs`
- API em .NET 10 LTS; container roda como `app` (C-7)
- Keep-alive no cron-job.org (AD-016); `keep-api-awake.yml` ficou só manual
- next 16.4.2, vitest 5, Firestore 4.5.0, Scalar 2.18.0; 8 PRs do Dependabot mergeados
- C-8 (AD-017), C-9, C-10 (feature `contact-anti-spam`), C-13 e quase todo o C-11

## Em Andamento

- Nada.

## Pendente

- Env var `AllowedHosts=portfolioapi-p03p.onrender.com` no Render (último item do C-11)
- C-12 no console: regras do Firestore, papel da service account, escopo da chave do Resend
- Conferir no log de deploy do Render: imagem `sdk:10.0` e processo rodando como `app`

## Bloqueios

- Nenhum. Os itens pendentes dependem de acesso aos consoles, que só o Pedro tem.

## Contexto

- Branch: `main`, igual a `origin/main`
- Não commitado: nada
- Decisões relacionadas: AD-016 (keep-alive), AD-017 (CSP); aceitos sem ação: `braces` em dev
  sem versão corrigida, TypeScript preso em 5.x no Dependabot
