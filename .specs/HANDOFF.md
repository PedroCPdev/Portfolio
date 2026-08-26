# HANDOFF.md

> Ponteiro para retomar rápido, não resumo completo. Sempre sobrescrito — o histórico é o `JOURNAL.md`.

**Data/sessão:** 2026-08-26
**Branch:** `main`, **14 commits à frente de `origin/main`** (nada foi enviado ainda)
**Feature ativa:** nenhuma. `admin-panel` e C-5 fecharam; o próximo item é **C-6**.

## Concluído

- **C-5 — validação de entrada no `POST /api/contact` — fechado em 2026-08-26.**
  `ContactMessage` declara os limites (80/254/4000 + formato de e-mail), o
  `ValidationFilter<ContactMessage>` os executa e o corpo tem teto de 32 KB. 15 testes novos
  (`ContactEndpointTests.cs`), CT-01..CT-05 verificados na matriz do `spec.md`, e o `413` com
  evidência contra Kestrel real. Gate `quick`: API 71 → **86**, zero pulados.
- **Feature `admin-panel` (T1..T13) — fechada.** Senha PBKDF2 + JWT na própria API, escrita
  autenticada em `/api/projects`, painel `/admin` no frontend com editor de decisões e
  confirmação de exclusão por título. 22/22 requisitos verificados na matriz do `spec.md`.
  Gate `full` verde e revalidado em 2026-08-26 após o reboot:
  71 testes na API + 88 no frontend = **159**, zero pulados
  (`features/admin-panel/evidencias/20260824-gate-full-APROVADO.md`).
  Decisões em `STATE.md` → AD-012, AD-013, AD-014. Lições novas → L-008, L-009, L-010.
- **A sessão de 2026-08-24 foi interrompida por reboot entre o gate e o commit da T13.**
  O que faltava era só o fechamento documental; foi retomado e commitado em 2026-08-26.

## Pendente (próximo passo)

**C-6 — rate limit global, não por visitante.** `Program.cs:93-99` usa uma janela fixa de 3
requisições/min para o endpoint `/api/contact` **inteiro**: um visitante consome a cota de
todos, e o excedente recebe `503` (não `429`). O login já tem política própria com `429`
(`Auth/LoginRateLimitPolicy.cs`), que serve de modelo. Nada foi iniciado.

Nota de contexto para quem for atacar o C-6: os testes de contato dependem do limite atual —
`ContactEndpointTests` sobe **uma fábrica por teste** justamente porque a janela é por
aplicação (design D-7 da `contact-input-validation`). Se o C-6 particionar por IP, essa
precaução pode ser simplificada.

Depois do C-6, o backlog segue em `codebase/CONCERNS.md` → C-7..C-13, de cima para baixo.

## Bloqueios ativos

Nenhum.

## Decidido nesta sessão

`frontend/assets/files.zip` (28 KB, binário) entrou no repositório por engano no commit
`974d1df` — estava não rastreado de propósito desde 2026-08-20. Levado ao usuário em
2026-08-26; ele decidiu **deixar como está**. Não é para "consertar" numa sessão futura.

## Não commitado

Nada. `.specs/features/contact-input-validation/` entrou no commit do C-5, como planejado.

## Fora do repositório, esperando o usuário

- **Deploy da `admin-panel` depende de duas variáveis novas no Render:**
  `Admin__PasswordHash` (gerado por `dotnet run -- hash-password <senha>`) e
  `Admin__JwtSigningKey` (≥ 32 bytes). Sem elas a área admin fica **fechada** — login e escrita
  respondem 401. Ver AD-013 e `codebase/INTEGRATIONS.md`.
- C-12: confirmar no console que as regras do Firestore negam acesso de cliente
  (`allow read, write: if false;`) e que a service account tem papel *Cloud Datastore User*,
  não Editor/Owner.
