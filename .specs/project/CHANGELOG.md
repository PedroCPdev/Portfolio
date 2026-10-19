# CHANGELOG

Histórico de entregas, mais recente no topo. Entradas anteriores nunca são reescritas.

## 2026-10-19 — Container da API roda sem root (quick/001)

A API passa a rodar como o usuário `app` (UID 1654) dentro do container, e a imagem perde `netbase` e `iproute2`. Resolve o C-7. `e3ef82d`. Detalhes em `.specs/quick/001-container-sem-root/SUMMARY.md`.
