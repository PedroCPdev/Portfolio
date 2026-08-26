# Gate full — APROVADO (2026-08-26)

Feature `contact-rate-limit-por-visitante` (C-6). Saídas reais, copiadas da execução.

## RED — antes da implementação

```
$ dotnet test PortfolioApi.Tests --filter "FullyQualifiedName~ContactRateLimitTests"

Failed!  - Failed: 7, Passed: 1, Skipped: 0, Total: 8
```

Amostras da falha, que mostram o comportamento antigo sendo medido:

```
Acima_do_teto_do_endpoint_ate_um_ip_novo_recebe_429
  Assert.Equal() Failure: Values differ   Expected: 20   Actual: 3

Recusa_do_proprio_visitante_nao_consome_o_teto_do_endpoint
  Expected: [OK, OK, OK]
  Actual:   [ServiceUnavailable, ServiceUnavailable, ServiceUnavailable]

Particao_usa_o_ip_mais_a_direita_do_x_forwarded_for
  Expected: TooManyRequests   Actual: ServiceUnavailable
```

O único que já passava no RED é `Health_nao_e_afetado_pelo_limitador_do_contato` (RL-07): hoje
`/health` não é limitado e precisa continuar não sendo depois que o limitador virou
`GlobalLimiter`. É guarda de regressão, não requisito dirigido por RED.

## GREEN + gate `full`

Emulador do Firestore no ar (`FIRESTORE_EMULATOR_HOST=127.0.0.1:8080`, JDK 25).

```
$ dotnet test PortfolioApi.Tests
Passed!  - Failed: 0, Passed: 94, Skipped: 0, Total: 94, Duration: 9 s

$ npm test        (frontend)
Test Files  7 passed (7)
     Tests  88 passed (88)

$ npm run build   (frontend)
Route (app)                           Revalidate  Expire
┌ ○ /                                         1m      1y
├ ○ /admin
└   /projects/[id]  ● 3 páginas
exit 0

$ npm run lint
exit 0
```

**182 testes, zero pulados** — 94 na API (86 antes + 8 desta feature) e 88 no frontend.
Nenhum teste existente foi alterado em asserção: o diff em `ContactEndpointTests.cs` é o docblock
e a extração da fábrica/espião para `ContactApiFactory.cs`, com a contagem daquela classe
inalterada.
