# Evidência — Gate da API (APROVADO)

Data: 2026-08-18
Ambiente: emulador Firestore em 127.0.0.1:8080 (firebase-tools, JDK 25), .NET 9.0.119

## `dotnet build` (PortfolioApi)
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## `dotnet test` (PortfolioApi.Tests)
```
Passed!  - Failed: 0, Passed: 12, Skipped: 0, Total: 12, Duration: 314 ms
```
Nenhum teste pulado ou desabilitado.

## Teste de mutação — prova de que a suíte detecta regressão

A implementação precedeu os testes, então o RED clássico não se aplicou. Em vez de presumir
que os testes são fortes, cada invariante foi quebrada de propósito:

| Mutação | Resultado | Leitura |
|---|---|---|
| M1 `OrderByDescending("createdAt")` → `OrderBy` | **Failed: 1** | ordenação é coberta de fato |
| M2 remover normalização UTC do setter de `CreatedAt` | **Failed: 2** | FS-05 é coberto de fato |
| M3 remover a checagem `snapshot.Exists` | **Passed: 12** | ver análise abaixo |
| M4 remover a guarda de idempotência do seed | **Failed: 2** | FS-06 é coberto de fato |
| restaurado | **Passed: 12** | sem efeito residual das mutações |

### Análise do M3 — mutação equivalente, não lacuna de cobertura
Verificado empiricamente contra o emulador:
```
Exists = False
ConvertTo<Project>() em doc inexistente => NULL
```
`DocumentSnapshot.ConvertTo<T>()` já devolve `null` para documento inexistente quando `T` é
tipo de referência. Logo a mutação **não muda o comportamento observável** — é uma mutação
equivalente, resultado esperado em análise de mutação, e não um teste fraco.
A checagem `snapshot.Exists` foi mantida por ser explícita quanto à intenção.
