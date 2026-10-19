# Anti-spam no formulário de contato (C-10)

**Data:** 2026-10-19
**Escopo:** Médio
**Spec:** `.specs/features/contact-anti-spam/spec.md`

## O que foi entregue

O formulário ganhou um campo que pessoas não veem nem alcançam pelo teclado. Robô que preenche
todos os campos preenche esse também, e a API descarta a mensagem respondendo como se tivesse
enviado (AS-01, AS-02).

A API passa a enviar no máximo 50 e-mails por dia UTC. Acima disso, o visitante recebe um aviso
que aponta para GitHub e LinkedIn, e a cota de 100 e-mails/dia do Resend fica preservada (AS-03,
AS-04). Mensagens recusadas por validação, rate limit, honeypot ou falta de configuração não
contam para o teto (AS-05).

## O que foi removido

Nada foi removido.

## Decisões

Honeypot com teto diário, escolhido pelo usuário entre três opções. O captcha (Cloudflare
Turnstile) exigiria script externo e mudança na CSP do AD-017. O contador fica em memória e zera
num reinício; a folga de 50 e-mails cobre um reinício por dia.

## Verificação

`node scripts/verify.mjs build`: 96 testes no frontend, 104 na API (67 + 37), zero pulados.
No Chromium, o campo fica fora da tela em 375 e 1440 px, o Tab não chega nele e a largura da
página não muda.

## Apêndice técnico

`7f751df`. `Services/DailyEmailBudget.cs` (novo), `Endpoints/ContactEndpoints.cs`,
`Models/ContactMessage.cs`, `Program.cs`, `appsettings.Example.json`,
`frontend/src/components/ContactForm.tsx`, `frontend/src/lib/api.ts`. Testes:
`ContactAntiSpamTests.cs`, `DailyEmailBudgetTests.cs`, `ContactForm.test.tsx`.
