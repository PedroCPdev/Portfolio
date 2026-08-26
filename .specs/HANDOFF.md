# HANDOFF.md

> Ponteiro para retomar rápido, não resumo completo. Sempre sobrescrito — o histórico é o `JOURNAL.md`.

**Data/sessão:** 2026-08-26
**Branch:** `main`, **15 commits à frente de `origin/main`** (nada foi enviado ainda)
**Feature ativa:** nenhuma. C-5 e C-6 fecharam; o próximo item é **C-7**.

## Concluído

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
transparente.

Depois do C-7, o backlog segue em `codebase/CONCERNS.md` → C-8..C-13, de cima para baixo.

## Bloqueios ativos

Nenhum.

## Decidido nesta sessão

Entre três desenhos para o C-6, o usuário escolheu o **encadeado** (3/min por visitante + teto de
20/min) em vez de só particionar por IP. O motivo: partição por IP sozinha deixaria quem alcança a
API sem passar pelo proxy do Render forjar o `X-Forwarded-For` e enviar sem limite nenhum, e o
endpoint dispara e-mail de verdade. O custo aceito é o teto ainda poder ser esgotado — o C-6 sobe
a barra de 3 para 20 requisições, não desaparece.

## Não commitado

Nada.

## Fora do repositório, esperando o usuário

- **Confirmar em produção que o proxy do Render anexa `X-Forwarded-For`.** Não é verificável a
  partir do repositório. Se ele não anexar, o limitador degrada para uma partição só (o
  comportamento anterior ao C-6) — nunca para "sem limite". Teste: quatro envios seguidos do
  formulário em menos de um minuto de um dispositivo, e um envio de outra rede logo depois; o
  segundo dispositivo tem de passar.
- **Deploy da `admin-panel` depende de duas variáveis novas no Render:**
  `Admin__PasswordHash` (gerado por `dotnet run -- hash-password <senha>`) e
  `Admin__JwtSigningKey` (≥ 32 bytes). Sem elas a área admin fica **fechada** (401).
- C-12: confirmar no console que as regras do Firestore negam acesso de cliente
  (`allow read, write: if false;`) e que a service account tem papel *Cloud Datastore User*.
