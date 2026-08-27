# HANDOFF.md

> Ponteiro para retomar rápido, não resumo completo. Sempre sobrescrito — o histórico é o `JOURNAL.md`.

**Data/sessão:** 2026-08-27
**Branch:** `main`, **sincronizado com `origin/main`** — os 16 commits foram enviados e estão em produção
**Feature ativa:** nenhuma. C-5 e C-6 fecharam; o próximo item é **C-7**.

## Concluído

- **Deploy do painel admin — confirmado em produção em 2026-08-27.** As duas variáveis existem no
  Render, `AdminOptions.IsConfigured` é verdadeiro e a área abriu: login devolve token e o cadastro
  de projeto pela tela funciona (confirmado pelo usuário). Medido daqui: `/admin` responde `200`
  com `noindex, nofollow, nocache`, e a home não tem nenhuma ocorrência de `/admin` no HTML.
  O `frontend/assets/files.zip`, que estava sobrando na árvore desde o `974d1df`, saiu no `41cfa76`.
- **C-6 — rate limit por visitante — fechado em 2026-08-26.**
  `POST /api/contact` tem duas camadas encadeadas (3/min por IP + 20/min no endpoint) registradas
  como `GlobalLimiter`, e o excedente responde `429`. O IP vem do `X-Forwarded-For` via
  `UseForwardedHeaders` configurado (`ForwardLimit = 1`, listas de proxy limpas). 8 testes novos
  (`ContactRateLimitTests.cs`), RL-01..RL-07 verificados na matriz. Gate `full`: API 86 → **94**,
  frontend 88, zero pulados. Decisão em AD-015; lições L-013 e L-014.
- **C-5 — validação de entrada no `POST /api/contact` — fechado em 2026-08-26.**
  Limites 80/254/4000 + formato de e-mail no `ContactMessage`, executados pelo
  `ValidationFilter`, corpo com teto de 32 KB. 15 testes.
- **Feature `admin-panel` (T1..T13) — fechada em 2026-08-24/26.** Senha PBKDF2 + JWT, escrita
  autenticada em `/api/projects`, painel `/admin`. 22/22 requisitos verificados.

## Pendente (próximo passo)

**C-7 — container roda como root.** `PortfolioApi/Dockerfile:8-13` não define `USER $APP_UID`, e
a imagem final carrega `iproute2`, ferramenta de diagnóstico de rede sem uso em runtime. Nada foi
iniciado. É a primeira tarefa do backlog que mexe em Docker e não em código de aplicação — vale
verificar se o Render reconstrói a imagem sem intervenção antes de assumir que o deploy é
transparente. O deploy de 08-27 é evidência parcial de que reconstrói, mas foi disparado por
commit, não por mudança só de Dockerfile.

Depois do C-7, o backlog segue em `codebase/CONCERNS.md` → C-8..C-13, de cima para baixo.

## Bloqueios ativos

Nenhum.

## Decidido nesta sessão

O envio ao `origin` é do usuário, não do agente: o hook `~/.claude/hooks/block-dangerous-git.sh`
barra `git ...` de envio partindo do Claude. O agente prepara commit e gate; quem empurra é o
usuário. Vale para as próximas sessões — não tente, o hook recusa.

## Não commitado

Nada.

## Fora do repositório, esperando o usuário

- **Conferir se a senha de admin e a chave de assinatura em uso não são as que apareceram no chat
  em 2026-08-27.** Durante a configuração foram impressos um hash de exemplo (senha literal
  `exemplo-descartavel`) e uma `JwtSigningKey`, ambos em texto puro numa conversa. Se qualquer um
  dos dois foi colado no Render, é segredo publicado: regerar com
  `dotnet run -- hash-password <senha>` e `openssl rand -base64 48`, e trocar a variável.
- **Confirmar em produção que o proxy do Render anexa `X-Forwarded-For`.** Não é verificável a
  partir do repositório. Se ele não anexar, o limitador degrada para uma partição só (o
  comportamento anterior ao C-6) — nunca para "sem limite". Teste: quatro envios seguidos do
  formulário em menos de um minuto de um dispositivo, e um envio de outra rede logo depois; o
  segundo dispositivo tem de passar.
- C-12: confirmar no console que as regras do Firestore negam acesso de cliente
  (`allow read, write: if false;`) e que a service account tem papel *Cloud Datastore User*.
