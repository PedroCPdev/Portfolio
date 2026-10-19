# Tarefa Rápida 001: Container da API roda sem root (C-7)

**Data:** 2026-10-19

## O que mudou

A API no Render rodava como root dentro do container. Agora roda como o usuário `app`, sem privilégio, que a imagem oficial do .NET já traz.

## O que foi removido

Os pacotes `netbase` e `iproute2` saíram da imagem final. Eles foram instalados em 2026-06 para o driver do Postgres resolver DNS, e o Postgres saiu na migração para o Firestore (AD-001).

## Decisões

Nenhuma. A remoção dos pacotes já constava no C-7.

## Verificação

**Comando:** `docker build` + `docker run` contra o emulador do Firestore
**Resultado:** `uid=1654(app)`; `/health` 200; seed gravou 2 projetos; `GET /api/projects` devolveu 2.
**Cenários cobertos:** sucesso.

## Commit

`e3ef82d` — `fix(api): rodar o container como usuario sem privilegio`
**Arquivos:** `PortfolioApi/Dockerfile`
