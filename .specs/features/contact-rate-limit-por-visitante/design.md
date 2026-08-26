# Design: rate limit do contato por visitante (C-6)

Toda afirmação sobre a API do .NET aqui tem execução por trás, em
`evidencias/20260826-cadeia-verificacao-rate-limit.md`. As referências `(P-N)` apontam para o
probe correspondente.

## D-1: o IP do visitante vem do `X-Forwarded-For`, com as listas de proxy limpas

`ForwardedHeadersOptions` sai da caixa com `ForwardedHeaders = None` e `KnownProxies = [::1]`
(P1). Chamar `app.UseForwardedHeaders()` sem configurar é inerte atrás do Render (P3a) — a
observação do próprio C-6, agora medida. A configuração adotada:

- `ForwardedHeaders = XForwardedFor` — só este cabeçalho; `X-Forwarded-Proto`/`Host` não têm uso
  aqui e cada um que se liga é superfície nova.
- `KnownProxies.Clear()` + `KnownNetworks.Clear()` — o container do Render só é alcançável pelo
  proxy deles; exigir o IP do proxy numa lista significaria mantê-la à mão para um endereço que
  a plataforma não promete estável.
- `ForwardLimit` fica no padrão **1**, explicitamente escrito no código com o porquê: é o que
  faz o middleware ler a entrada **mais à direita** — a anexada pelo proxy imediato. Com `2` ele
  anda um salto para a esquerda e passa a acreditar no que o cliente escreveu (P3d).

**Isto revoga a premissa da D-10 do `admin-panel`** ("particionar por `X-Forwarded-For` seria
falsa sensação de segurança"). A premissa vale para o valor mais à esquerda, que é do cliente;
não vale para o mais à direita, que é do proxy. A D-10 continua correta na conclusão para o
login apenas porque nada mudou lá.

**Custo assumido:** com as listas limpas, quem alcançar a aplicação **sem** passar pelo proxy
(porta local em desenvolvimento; algum caminho interno do Render) pode escrever qualquer
`X-Forwarded-For` e ganhar a partição que quiser. É o que o teto do D-3 existe para conter.

## D-2: partição por IP, teto global, e o encadeamento como `GlobalLimiter`

A ordem importa e não é intuitiva. Com o teto em `RateLimiterOptions.GlobalLimiter` e a partição
por IP como política do endpoint, o middleware adquire o global primeiro — e uma requisição
recusada depois, pela política do endpoint, **consome 2 permits do teto** contra 1 da aceita
(P9, P10). Um atacante sozinho esgotaria o teto em ~11 requisições e derrubaria o formulário
para todos: o C-6 reaparecendo pela porta dos fundos.

Adotado: `PartitionedRateLimiter.CreateChained(porIp, teto)` **inteiro** como `GlobalLimiter`,
sem `RequireRateLimiting` no endpoint. O encadeamento adquire na ordem declarada — por-IP
primeiro, teto só se aquele passar —, então recusa por IP não toca o teto (P11).

**Custo:** `/api/contact` deixa de declarar `.RequireRateLimiting("contact")` na própria linha,
que era onde o limite ficava visível para quem lê o endpoint. Fica um comentário no lugar,
apontando para `RateLimiting/ContactRateLimiter.cs`, e a rota vira `ContactEndpoints.Rota` —
uma constante só, consumida pelos dois lados, para que renomear a rota não desligue o limitador
em silêncio.

## D-3: 3/min por visitante, 20/min no endpoint inteiro

O 3/min é o número de hoje, preservado — o C-6 é sobre *de quem* é a cota, não sobre o tamanho
dela. O teto de 20/min é o backstop do D-1: alto o bastante para caber 6 visitantes legítimos
simultâneos com a cota cheia, baixo o bastante para que uma falsificação de cabeçalho não vire
envio ilimitado pelo Resend. Constantes no código, como no login (`LoginRateLimitPolicy:31`).

Nota de proporção: nenhum dos dois números é guarda de cota do Resend (100 e-mails/dia no free
tier) — 20/min saturaria a cota diária em 5 minutos. O que protege cota é o C-10 (anti-spam),
item seguinte do backlog. Este teto protege disponibilidade e o custo de saída, não a cota.

## D-4: `429` global em vez de política própria

`RateLimiterOptions.RejectionStatusCode = 429` (P4). A D-10 do `admin-panel` criou
`LoginRateLimitPolicy.OnRejected` **justamente** para não mexer no `503` do contato; agora o
contato também quer `429`, então a razão de existir daquele efeito colateral evitado acabou.

O `OnRejected` do login **não** é removido nesta tarefa: é redundante, não errado, e mexer nele
é churn em arquivo fora do escopo do C-6. O que muda lá é o comentário XML que afirmava ser
falso isolamento particionar por `X-Forwarded-For` — comentário factualmente incorreto depois do
D-1, e comentário errado é armadilha para a próxima sessão.

`Retry-After` não sai de graça: o fixed window não emite a metadata (P4). Fora de escopo.

## D-5: `UseForwardedHeaders` é o primeiro middleware da FASE 3

Precisa rodar antes de `UseRateLimiter` (o limitador lê `RemoteIpAddress`) e antes de qualquer
log que cite IP. Vai no topo do pipeline, antes inclusive do CORS.

## D-6: partições ociosas não são vazamento

`DefaultPartitionedRateLimiter` descarta partição parada há 10 s (`s_idleTimeLimit`, P7). Uma
varredura de IPs não faz o dicionário crescer sem fim, então não há necessidade de limitar o
número de partições à mão.

## Análise de reúso

- **Padrão de política de rate limit:** `Auth/LoginRateLimitPolicy.cs:28-35` — o formato do
  `FixedWindowRateLimiterOptions` (janela, `QueueProcessingOrder`, `QueueLimit = 0`) é copiado
  dali, inclusive o `QueueLimit = 0` que é o que faz o excedente ser recusado em vez de
  enfileirado.
- **Teste por HTTP contra a aplicação real:** `PortfolioApi.Tests/ContactEndpointTests.cs:157`
  (`ContactApiFactory` + `ResendSpy`) é reaproveitado inteiro — o espião de HTTP de saída é o que
  permite afirmar "não chamou o Resend" sem tocar a rede (L-011, L-012).
- **Convenção de nomeação e comentário:** classes de política com XML doc explicando o *porquê*,
  como em `LoginRateLimitPolicy`.

## Pontos que ficaram incertos

- **Por que a recusa consome 2 permits do teto** (P10) é comportamento medido, não explicado: não
  fui atrás do código do `RateLimitingMiddleware` para nomear a causa. O desenho adotado não
  depende da explicação — depende de o encadeamento nunca chegar ao teto quando o por-IP recusa,
  que é o que o P11 mede diretamente.
- **Se o proxy do Render de fato anexa `X-Forwarded-For`** não é verificável a partir daqui. Se
  não anexar, `RemoteIpAddress` continua sendo o do proxy e o comportamento degrada para o de
  hoje (uma partição para todos, RL-06) — nunca para "sem limite". Confirmação em produção fica
  como passo fora do repositório, no relatório.
