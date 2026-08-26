# Spec: rate limit do contato por visitante (C-6)

Escopo classificado como: **Médio** — um componente (a API), sem dependência nova, com decisão
de desenho real (ordem de aquisição dos limitadores) e superfície de rede afetada.

Origem: `codebase/CONCERNS.md` → C-6. Hoje `AddFixedWindowLimiter("contact", ...)` cria uma
**única partição**: os 3 req/min valem para o endpoint inteiro somando todos os visitantes, e o
excedente recebe `503`. Três requisições de um script deixam o formulário indisponível para todo
mundo até a janela virar.

## Requisitos

### P1 — MVP (fatia vertical, demonstrável ponta a ponta)

**[RL-01]**
QUANDO um visitante envia mais de 3 requisições a `POST /api/contact` dentro de uma janela de
1 minuto
ENTÃO o sistema DEVE recusar as excedentes com **429** (não `503`) e não chamar o Resend.

**[RL-02]**
QUANDO um visitante esgota a própria janela
ENTÃO um visitante de outro IP DEVE continuar recebendo `200` dentro da janela dele.
*(É o coração do C-6: um não paga pelo outro.)*

**[RL-03]**
QUANDO o total de requisições **aceitas** no endpoint atinge 20 numa janela de 1 minuto
ENTÃO o sistema DEVE recusar com `429` inclusive um IP que ainda não apareceu.
*(Backstop: sem ele, quem alcançar a API sem passar pelo proxy pode girar o `X-Forwarded-For` e
ganhar requisições ilimitadas — o endpoint manda e-mail de verdade.)*

**[RL-04]**
QUANDO uma requisição é recusada pela janela do próprio visitante
ENTÃO o teto global NÃO DEVE ser consumido.
*(Sem isto o atacante esgota o teto e reproduz o C-6 com a barra 6× mais alta — medido em
`evidencias/`, P9/P10.)*

**[RL-05]**
QUANDO a requisição chega com `X-Forwarded-For` contendo mais de um endereço
ENTÃO a partição DEVE ser o endereço **mais à direita** (o que o proxy anexou), ignorando o que
o cliente escreveu à esquerda.

**[RL-06]**
QUANDO a requisição chega sem `X-Forwarded-For`, ou com valor malformado
ENTÃO o sistema DEVE cair no IP da conexão e continuar limitando — nunca liberar sem limite.

**[RL-07]**
QUANDO a rota não é `/api/contact` (`/health`, `/api/projects`, `/api/admin/login`)
ENTÃO nenhum dos dois limitadores desta feature DEVE ser aplicado.

## Fora de escopo

- **Cabeçalho `Retry-After` na resposta 429.** O fixed window não emite a metadata (verificado,
  P4); produzi-la exigiria calcular o fim da janela à mão. Vira Ideia Adiada.
- **Rate limit do login por IP.** `LoginRateLimitPolicy` continua com partição única. O custo
  (alguém esgota a janela e deixa o admin 1 min sem login) segue registrado; estender é tarefa
  própria, não "já que estou aqui".
- **Anti-spam (honeypot/captcha).** É o C-10, item seguinte do backlog. Rate limit não impede
  robô educado.
- **Limites por configuração.** Os números ficam como constantes no código, como já são hoje e
  como é no login.
- **Mexer no `next.config.ts` ou em qualquer coisa do frontend.** O formulário não muda: ele já
  trata falha de envio, e `429` cai no mesmo caminho de erro que o `503` caía.

## Matriz de rastreabilidade

| ID | Tarefa | Implementação | Teste | Evidência | Status |
|---|---|---|---|---|---|
| RL-01 | T2 | `RateLimiting/ContactRateLimiter.cs:26,50` + `Program.cs:116` | `ContactRateLimitTests.cs:53` | ✓ | Verificado |
| RL-02 | T2 | `RateLimiting/ContactRateLimiter.cs:50-56` | `ContactRateLimitTests.cs:69` | ✓ | Verificado |
| RL-03 | T2 | `RateLimiting/ContactRateLimiter.cs:34,58` | `ContactRateLimitTests.cs:84` | ✓ | Verificado |
| RL-04 | T2 | `RateLimiting/ContactRateLimiter.cs:48` (ordem do encadeamento) | `ContactRateLimitTests.cs:109` | ✓ | Verificado |
| RL-05 | T2 | `Program.cs:98-109` (`ForwardLimit = 1`, listas limpas) | `ContactRateLimitTests.cs:127` | ✓ | Verificado |
| RL-06 | T2 | `RateLimiting/ContactRateLimiter.cs:37,53` | `ContactRateLimitTests.cs:146` | ✓ | Verificado |
| RL-07 | T2 | `RateLimiting/ContactRateLimiter.cs:66` (`EhOContato`) | `ContactRateLimitTests.cs:160` | ✓ | Verificado |

Status possíveis: Não iniciado · Em andamento · Verificado · Não atendido

Evidência do gate: `evidencias/20260826-gate-full-APROVADO.md`.
Cadeia de verificação da API do .NET: `evidencias/20260826-cadeia-verificacao-rate-limit.md`.
