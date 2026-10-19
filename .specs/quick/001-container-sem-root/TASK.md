# Tarefa Rápida 001: Container da API roda sem root (C-7)

**Data:** 2026-10-19
**Status:** Concluída

## Descrição

A imagem Docker da API rodava como root e carregava `netbase` e `iproute2`, que sobraram da época do Postgres.

## Arquivos Alterados

- `PortfolioApi/Dockerfile`: `USER $APP_UID` na imagem final; sai o `apt-get install netbase iproute2`.

## Verificação

- [x] `docker run --entrypoint id` mostra `uid=1654(app) gid=1654(app)`.
- [x] `ip` não existe mais na imagem (`command -v ip` vazio).
- [x] Container contra o emulador do Firestore: `/health` responde `{"status":"ok"}`, o seed grava 2 projetos e `GET /api/projects` devolve os 2 com id de 20 caracteres.

**Cenários cobertos:** sucesso (API sobe como `app` e fala gRPC com o Firestore sem `netbase`). Sem sucesso e falha não se aplicam: a mudança não tem entrada de usuário nem caminho de erro novo.
**Evidência:** output de `docker run` nesta sessão: `uid=1654(app)`, `projetos: 2 [ 20, 20 ]`, log `[Seeder] 2 projetos semeados no Firestore.`

## Commit

`e3ef82d` — fix(api): rodar o container como usuario sem privilegio
