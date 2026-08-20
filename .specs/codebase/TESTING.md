# TESTING.md

## Estado
- `frontend/` — **vitest** (`npm test`). Gate = `npm test` + `npm run build` + `npm run lint`.
- `PortfolioApi/` — **xUnit em `PortfolioApi.Tests/`** (introduzido na migração Firestore).

## Comandos de gate

| Gate | Comando | Cobre |
|---|---|---|
| `build` | `cd PortfolioApi && dotnet build` | compila a API |
| `quick` | `cd PortfolioApi.Tests && dotnet test` | unit + integração (exige emulador) |
| `full` | `quick` + `cd frontend && npm test && npm run build && npm run lint` | tudo (40 testes: 18 na API, 22 no frontend) |

## Emulador Firestore (obrigatório para os testes de integração)

```bash
export JAVA_HOME=/usr/lib/jvm/java-25-openjdk   # firebase-tools exige JDK 21+
export PATH="$JAVA_HOME/bin:$PATH"
npx -y firebase-tools@latest emulators:start --only firestore --project demo-portfolio
export FIRESTORE_EMULATOR_HOST=127.0.0.1:8080
```

Os testes de integração **falham** (não são pulados) se o emulador não estiver no ar —
skip silencioso mascararia regressão real.

## Testes de contrato sem emulador
`PortfolioApi.Tests/ProjectContractTests.cs` serializa `Project` com `JsonSerializerDefaults.Web`
e checa o nome dos campos no JSON. Nao toca no Firestore e roda sem emulador. Existe porque nao ha
DTO (C-4): renomear uma propriedade do model muda o contrato publico sem o compilador reclamar.

`frontend/src/__tests__/design-system.test.ts` guarda as invariantes do sistema de design lendo os
arquivos-fonte (tokens, ausencia de hex legado, foco, movimento reduzido, reserva do `--clay`).
Tambem roda sem emulador e sem jsdom.

## Isolamento
Cada teste de integração usa uma coleção com sufixo único (GUID) para ser parallel-safe.
