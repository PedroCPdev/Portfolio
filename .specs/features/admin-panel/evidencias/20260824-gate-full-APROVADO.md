# Gate completo — Painel administrativo (APROVADO)

**Data:** 2026-08-24 · **Feature:** `admin-panel` · **Escopo:** Grande

**Contagem:** API 20 → **71** · frontend 39 → **88** · total 59 → **159**. Nenhum teste
removido, pulado ou desabilitado no caminho; a suíte só cresceu.

**Emulador Firestore no ar** (`127.0.0.1:8080`, JDK 25 conforme L-003) — os testes de
integração falham alto sem ele, nunca são pulados.

```
$ cd PortfolioApi && dotnet build
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.47

$ cd PortfolioApi.Tests && FIRESTORE_EMULATOR_HOST=127.0.0.1:8080 dotnet test
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    71, Skipped:     0, Total:    71, Duration: 2 s - PortfolioApi.Tests.dll (net9.0)

$ cd frontend && npm test
 ✓ src/lib/__tests__/api.test.ts (5 tests) 19ms

 Test Files  7 passed (7)
      Tests  88 passed (88)
   Start at  15:25:52
   Duration  2.86s (transform 479ms, setup 0ms, collect 1.47s, tests 2.64s, environment 5.14s, prepare 992ms)


$ cd frontend && npm run build
✓ Compiled successfully in 484ms
Route (app)                           Revalidate  Expire
├ ○ /admin

$ cd frontend && npm run lint
> frontend@0.1.0 lint
> eslint

```

O `TypeError: fetch failed` que aparece durante `npm run build` quando a API local está
desligada é comportamento existente e proposital: `getProjects` tenta três vezes, engole a falha
e devolve `[]`, para que a API fora do ar não quebre o build (C-1 / AD-005). Não é regressão
desta feature.

---

## Revalidação em 2026-08-26 (após reboot da máquina)

A sessão de 2026-08-24 foi interrompida por um reinício do PC entre o gate e o commit da T13.
Nenhum arquivo de código mudou nesse intervalo (`git status` acusava só `.specs/` e `CLAUDE.md`),
mas o gate foi rodado de novo em vez de presumido:

```
$ cd frontend && npm test
 Test Files  7 passed (7)
      Tests  88 passed (88)
   Duration  7.09s

$ cd frontend && npm run build
✓ Generating static pages using 5 workers (8/8) in 9.5s
Route (app)                           Revalidate  Expire
├ ○ /admin

$ cd frontend && npm run lint   → exit 0

$ cd PortfolioApi && dotnet build
    0 Warning(s)
    0 Error(s)

$ cd PortfolioApi.Tests && FIRESTORE_EMULATOR_HOST=127.0.0.1:8080 dotnet test
Passed!  - Failed:     0, Passed:    71, Skipped:     0, Total:    71, Duration: 4 s
```

Mesmos números de 2026-08-24: 71 + 88 = **159**, zero pulados.
