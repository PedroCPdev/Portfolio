# CHANGELOG

Histórico de entregas, mais recente no topo. Entradas anteriores nunca são reescritas.

## 2026-10-19 — Anti-spam no formulário de contato (C-10)

Robô que preenche o campo escondido tem a mensagem descartada sem perceber, e a API envia no máximo 50 e-mails por dia. `7f751df`. Detalhes em `.specs/features/contact-anti-spam/RELEASE-NOTES.md`.

## 2026-10-19 — Formulário avisa quando o e-mail está desligado (C-9)

Sem a chave do Resend, o visitante passa a ver um erro que aponta para GitHub e LinkedIn, e a mensagem deixa de sumir em silêncio. O aviso de excesso de mensagens voltou a aparecer no 429. `c657179`.

## 2026-10-19 — Headers de segurança no frontend (C-8)

O site passa a proibir ser embutido em frame de outra origem e limita com quem o navegador conversa: a própria origem e a API. Também envia `nosniff`, `Referrer-Policy` e `Permissions-Policy`. `b039f3a`.

## 2026-10-19 — GET de projeto com barra no id responde 404 (C-13)

Um id com barra na URL deixava de responder 500 e passa a responder 404. `7d213c7`.

## 2026-10-19 — Achados baixos do C-11

O e-mail de quem usa o formulário saiu dos logs da API. O CI passa a falhar com dependência de produção vulnerável, e o Dependabot abre PR semanal. `d420813`, `ed4998c`.

## 2026-10-19 — Dependências atualizadas

next 16.4.2 fecha 3 falhas críticas de execução remota de código em produção. vitest 5.0.5, Firestore 4.5.0 e Scalar 2.18.0 no mesmo dia. `a1fdbac`, `3818349`, `89558b0`.

## 2026-10-19 — Container da API roda sem root (quick/001)

A API passa a rodar como o usuário `app` (UID 1654) dentro do container, e a imagem perde `netbase` e `iproute2`. Resolve o C-7. `e3ef82d`. Detalhes em `.specs/quick/001-container-sem-root/SUMMARY.md`.
