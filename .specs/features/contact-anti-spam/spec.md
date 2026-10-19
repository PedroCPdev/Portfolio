# Spec: anti-spam no formulário de contato (C-10)

Escopo classificado como: **Médio**. Toca API e frontend, sem dependência nova. A escolha entre
honeypot, captcha e teto foi do usuário em 2026-10-19: honeypot + teto diário.

Origem: `codebase/CONCERNS.md` → C-10. A única barreira é o rate limit do C-6 (3/min por IP,
20/min no endpoint). Um robô que respeite esse ritmo esgota a cota do Resend (100 e-mails/dia)
em menos de uma hora, e a partir daí nenhuma mensagem legítima sai até o dia seguinte.

## Requisitos

**[AS-01]**
QUANDO `POST /api/contact` chega com o campo `website` preenchido
ENTÃO o sistema DEVE responder `200 { success: true }` sem chamar o Resend e sem consumir o teto
diário.
*(O robô não pode distinguir a recusa de um envio. Responder erro ensinaria a deixar o campo vazio.)*

**[AS-02]**
O formulário DEVE ter um campo `website` que pessoa nenhuma vê nem alcança pelo teclado: fora da
tela, `tabIndex={-1}`, `autoComplete="off"` e `aria-hidden`. O valor dele DEVE ir no corpo do POST.

**[AS-03]**
QUANDO o número de e-mails enviados no dia UTC atinge `Contact:DailyEmailLimit` (padrão 50)
ENTÃO o sistema DEVE responder `503 { success: false, error }` sem chamar o Resend, com um texto
que leva o visitante para GitHub ou LinkedIn.

**[AS-04]**
QUANDO o dia UTC vira
ENTÃO o teto DEVE voltar a zero.

**[AS-05]**
Só a mensagem que vai de fato para o Resend consome o teto: recusa por validação (400), por rate
limit (429), por honeypot (AS-01) ou por e-mail sem configuração (503 do C-9) não conta.

## Fora de escopo

- Captcha (Cloudflare Turnstile): exige script externo, chave secreta e mudança na CSP do AD-017.
- Persistir o contador. Ele mora em memória e zera quando a instância reinicia. Com teto de 50
  contra a cota de 100, sobra espaço para um reinício por dia; o Render dorme das 00:00 às 08:00
  BRT, fora do horário em que o contador importa.
- Devolver o crédito quando o Resend falha (502): o Resend pode ter contado a tentativa.

## Rastreabilidade

| Requisito | Teste | Evidência |
| --- | --- | --- |
| AS-01 | `ContactAntiSpamTests.Honeypot_preenchido_responde_200_sem_chamar_o_resend` | gate full |
| AS-02 | `ContactForm.test.tsx` (campo escondido e enviado) | gate quick |
| AS-03 | `ContactAntiSpamTests.Teto_diario_atingido_responde_503_sem_chamar_o_resend` | gate full |
| AS-04 | `DailyEmailBudgetTests.Teto_volta_a_zero_quando_o_dia_utc_vira` | gate quick |
| AS-05 | `ContactAntiSpamTests.Honeypot_nao_consome_o_teto` + `DailyEmailBudgetTests` | gate quick |
