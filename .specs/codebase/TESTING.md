# TESTING.md

## Estado
- `frontend/` — **vitest** (`npm test`). Gate = `npm test` + `npm run build` + `npm run lint`.
- `PortfolioApi/` — **xUnit em `PortfolioApi.Tests/`** (introduzido na migração Firestore).

## Comandos de gate

Desde 2026-10-19 o gate é `node scripts/verify.mjs <nível>`, o mesmo comando dos hooks e do CI.

| Gate | Comandos | Cobre |
|---|---|---|
| `quick` | lint, `tsc --noEmit`, vitest, `dotnet test --filter Category!=Firestore` | 88 testes do frontend + 60 da API, ~30s |
| `full` | `quick` + `firebase emulators:exec` rodando `dotnet test --filter Category=Firestore` | + 34 testes de integração; sobe e derruba o emulador sozinho |
| `build` | `full` + `next build` + `dotnet build PortfolioApi -c Release` | tudo (**182 testes: 94 na API, 88 no frontend**, medido em 2026-10-19) |

Classe que usa `FirestoreFixture` precisa de `[Trait("Category", "Firestore")]`; sem ele, entra no `quick` e falha sem emulador.
O `verify.mjs` define `DOTNET_ROLL_FORWARD=Major`: numa máquina só com o runtime .NET 10, o testhost do net9.0 roda nele.

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

## Testes de componente (frontend)
Componentes React são testados com `@testing-library/react` sobre **jsdom ligado por arquivo**:
a primeira linha do teste é `// @vitest-environment jsdom`. O ambiente padrão continua sendo
`node`, para que os testes que leem código-fonte (`design-system`) e os de `lib/` não paguem o
custo do DOM. `vitest.config.ts` existe só para resolver o alias `@/` — sem ele o vitest resolve
import de tipo (que some no transform) mas não import de valor.

Componentes que falam com a API mockam `@/lib/admin` inteiro; nenhum teste de frontend faz rede.

## Isolamento
Cada teste de integração usa uma coleção com sufixo único (GUID) para ser parallel-safe.
