@AGENTS.md

## Específico do Claude Code

- Este projeto usa o plugin **pedro-harness**. Hooks ativos: bloqueio de comandos destrutivos e de segredos, `verify format` após cada edição e `verify quick` antes de encerrar o turno. Se um hook bloquear, corrija a causa — não contorne.
- Ao retomar trabalho, leia `.specs/project/STATE.md` e, se existir, `.specs/HANDOFF.md`.
