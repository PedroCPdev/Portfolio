# HANDOFF.md

> Ponteiro para retomar rápido, não resumo completo. Sempre sobrescrito — o histórico é o `JOURNAL.md`.

**Data/sessão:** 2026-08-26
**Branch:** `main`, **12 commits à frente de `origin/main`** (nada foi enviado ainda)
**Feature ativa:** nenhuma. A `admin-panel` fechou; a próxima é **C-5**, ainda não iniciada.

## Concluído

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

**C-5 — validação de entrada no `POST /api/contact`.** Ficou muito mais barato do que a spec de
2026-08-18 previa: a `admin-panel` já construiu o `ValidationFilter<T>`, o `NotBlankAttribute`,
a fábrica de teste HTTP (`PortfolioApi.Tests/AdminApiFactory.cs`) e o
`public partial class Program;`. **Releia `features/contact-input-validation/design.md` antes de
implementar — a spec e o design são anteriores a essas peças e superestimam o trabalho.**

O que sobrou de fato: anotar `Models/ContactMessage.cs` com DataAnnotations, pendurar
`ValidationFilter<ContactMessage>` em `Endpoints/ContactEndpoints.cs`, limitar o corpo, e os
testes de CT-01..CT-05. A pergunta de aprovação que travou a sessão de 08-18 (aceitar
`Microsoft.AspNetCore.Mvc.Testing` no projeto de teste) **já está respondida na prática**: a
dependência entrou com a `admin-panel` e está em uso.

Depois do C-5, o backlog segue em `codebase/CONCERNS.md` → C-6..C-13, de cima para baixo.

## Bloqueios ativos

Nenhum.

## Achado que precisa de decisão do usuário

`frontend/assets/files.zip` (28 KB, binário) entrou no repositório por engano no commit
`974d1df` — ele estava não rastreado de propósito desde 2026-08-20, para o usuário decidir se
entrava. Como os 12 commits ainda **não foram enviados**, dá para removê-lo reescrevendo o
histórico ou num commit novo de remoção. Não foi mexido: é decisão do usuário.

## Não commitado

- `.specs/features/contact-input-validation/` (spec.md + design.md), ainda não rastreados.
  Deliberado: vão no mesmo commit da implementação do C-5, para o commit contar uma história só.

## Fora do repositório, esperando o usuário

- **Deploy da `admin-panel` depende de duas variáveis novas no Render:**
  `Admin__PasswordHash` (gerado por `dotnet run -- hash-password <senha>`) e
  `Admin__JwtSigningKey` (≥ 32 bytes). Sem elas a área admin fica **fechada** — login e escrita
  respondem 401. Ver AD-013 e `codebase/INTEGRATIONS.md`.
- C-12: confirmar no console que as regras do Firestore negam acesso de cliente
  (`allow read, write: if false;`) e que a service account tem papel *Cloud Datastore User*,
  não Editor/Owner.
