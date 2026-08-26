# Cadeia de Verificação — rate limit por visitante (C-6)

Data: 2026-08-26. Todas as saídas abaixo são de execução real (`dotnet test` sobre um
`ProbeC6.cs` temporário, apagado depois de coletadas as evidências). Nenhuma afirmação
aqui vem de memória sobre a API do .NET.

Comando: `dotnet test PortfolioApi.Tests --filter "FullyQualifiedName~ProbeC6" --logger "console;verbosity=detailed"`

## P1 — defaults de `ForwardedHeadersOptions`

```
ForwardLimit      = 1
ForwardedHeaders  = None
KnownProxies      = [::1]
KnownNetworks     = [127.0.0.1/8]
RequireHeaderSymmetry = False
```

Consequência: `app.UseForwardedHeaders()` sem configuração **não faz nada** em dois níveis —
`ForwardedHeaders` é `None` (nenhum cabeçalho é processado) e, mesmo ligado, só confiaria num
proxy em loopback. Atrás do proxy do Render, que não é loopback, o middleware seria inerte.

## P2/P3 — o que o middleware faz com `X-Forwarded-For`

```
sem middleware, sem header        -> ip=null xff=
defaults + XFF '1.1.1.1, 2.2.2.2' -> ip=null   xff=1.1.1.1, 2.2.2.2   (inerte)
limpos + XFF '1.1.1.1, 2.2.2.2'   -> ip=2.2.2.2 xff=1.1.1.1
limpos + XFF '9.9.9.9'            -> ip=9.9.9.9 xff=
limpos + limit2 + XFF '1.1, 2.2'  -> ip=1.1.1.1 xff=
limpos, sem header                -> ip=null xff=
limpos + XFF 'nao-e-ip'           -> ip=null xff=nao-e-ip
```

Três fatos que decidem o desenho:

1. Com `KnownProxies`/`KnownNetworks` limpos, o middleware consome a entrada **mais à direita**
   do `X-Forwarded-For` — a que foi anexada pelo proxy imediato, não a que o cliente escreveu.
   É por isso que particionar por ela **não** é falso isolamento (revoga a premissa da D-10 do
   `admin-panel`).
2. `ForwardLimit = 2` faz o middleware andar dois saltos para a esquerda e passar a acreditar no
   valor escrito pelo cliente (`ip=1.1.1.1`). O padrão `1` é o que mantém a garantia — subir esse
   número é o modo de reintroduzir a falsificação.
3. Valor malformado não vira exceção nem partição lixo: o IP permanece o da conexão.

## P4 — partição por IP e status de rejeição

```
IP 1.1.1.1 x5 -> 200 200 200 429 429
IP 2.2.2.2 x2 -> 200 200
ultimo Retry-After: ausente
```

`RateLimiterOptions.RejectionStatusCode = 429` produz o `429`. O `Retry-After` **não** é emitido
pelo fixed window — fica como ideia adiada, não como requisito desta feature.

## P7 — partições ociosas não vazam memória

```
tipo concreto: System.Threading.RateLimiting.DefaultPartitionedRateLimiter`2[...]
  campo: TimeSpan s_idleTimeLimit = 00:00:10
```

Há um timer interno que descarta partição parada há 10 s. Particionar por IP não cria
crescimento ilimitado de memória sob varredura de IPs.

## P9/P10 — o achado que mudou o desenho

Com o teto global em `RateLimiterOptions.GlobalLimiter` e a partição por IP como política do
endpoint (`RequireRateLimiting`), o **middleware adquire o global primeiro**. Medindo o teto
requisição a requisição (por-IP = 1/min, teto = 20/min, mesmo IP 5×):

```
req1 -> 200   teto restante: 19
req2 -> 429   teto restante: 17
req3 -> 429   teto restante: 15
req4 -> 429   teto restante: 13
req5 -> 429   teto restante: 11
```

**Requisição recusada pela política do endpoint consome 2 permits do teto**, contra 1 da
requisição aceita. Efeito prático: um único atacante esgotaria o teto global em ~11 requisições
e derrubaria o formulário para todo mundo — exatamente o C-6 que a feature existe para fechar,
só que com a barra um pouco mais alta.

## P11 — a ordem que corrige (desenho adotado)

`PartitionedRateLimiter.CreateChained(porIp, teto)` inteiro como `GlobalLimiter`, sem política
no endpoint. O encadeamento adquire na **minha** ordem: por-IP primeiro, teto só se passar.

```
(d) chain(porIp=1/min, teto=20/min) como GlobalLimiter
  1.1.1.1 -> 200   teto restante: 19
  1.1.1.1 -> 429   teto restante: 19   <- recusa por IP não toca o teto
  1.1.1.1 -> 429   teto restante: 19
  1.1.1.1 -> 429   teto restante: 19
  2.2.2.2 -> 200   teto restante: 18   <- visitante novo passa
  /livre (rota sem limite) -> 200      <- rota fora do guard não é afetada
```

## P8 — o `GlobalLimiter` não vaza para as outras rotas

```
/livre x8 (mesmo ip, sem politica) -> 200 200 200 200 200 200 200 200
/livre depois do teto estourado    -> 200
```

Com `RateLimitPartition.GetNoLimiter` para todo caminho que não é o do contato, `/health`,
`/api/projects` e o login seguem sem limite algum vindo daqui.

## P5 — APIs confirmadas existirem (contra invenção de assinatura)

```
PartitionedRateLimiter`1[TResource] CreateChained[TResource](PartitionedRateLimiter`1[TResource][])
--- RateLimiterOptions ---
PartitionedRateLimiter`1 GlobalLimiter
Func`3 OnRejected
Int32 RejectionStatusCode
```
