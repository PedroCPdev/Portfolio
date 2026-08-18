# TESTING.md

## Estado
- `frontend/` — **vitest** (`npm test`). Gate = `npm test` + `npm run build` + `npm run lint`.
- `PortfolioApi/` — **xUnit em `PortfolioApi.Tests/`** (introduzido na migração Firestore).

## Comandos de gate

| Gate | Comando | Cobre |
|---|---|---|
| `build` | `cd PortfolioApi && dotnet build` | compila a API |
| `quick` | `cd PortfolioApi.Tests && dotnet test` | unit + integração (exige emulador) |
| `full` | `quick` + `cd frontend && npm test && npm run build && npm run lint` | tudo (17 testes) |

## Emulador Firestore (obrigatório para os testes de integração)

```bash
export JAVA_HOME=/usr/lib/jvm/java-25-openjdk   # firebase-tools exige JDK 21+
export PATH="$JAVA_HOME/bin:$PATH"
npx -y firebase-tools@latest emulators:start --only firestore --project demo-portfolio
export FIRESTORE_EMULATOR_HOST=127.0.0.1:8080
```

Os testes de integração **falham** (não são pulados) se o emulador não estiver no ar —
skip silencioso mascararia regressão real.

## Isolamento
Cada teste de integração usa uma coleção com sufixo único (GUID) para ser parallel-safe.
