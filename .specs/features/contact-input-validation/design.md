# Design — Validação de entrada no POST /api/contact

## Decisões arquiteturais

**D-1: validação declarativa via `IEndpointFilter` genérico, não checagem manual no endpoint.**
`Models/ContactMessage.cs:1` já importa `System.ComponentModel.DataAnnotations` e já tem
`[Required]` nos três campos — só falta alguém executá-los. Um `ValidationFilter<T>` que roda
`Validator.TryValidateObject` transforma os atributos existentes em validação de verdade, mantém
as regras declaradas no model (onde se lê o contrato) e serve qualquer endpoint futuro que receba
corpo. A alternativa — mais `if` em `ContactEndpoints` — espalharia regra de negócio pelo endpoint
e deixaria os atributos mentindo sobre o comportamento.

**D-2: o shape de erro continua `{ success, error }` — não usar `Results.ValidationProblem`.**
`CONVENTIONS.md` fixa esse shape para o contato, e `frontend/src/lib/api.ts:69-73` lê exatamente
`data.error` para exibir ao visitante. `ValidationProblem` devolveria RFC 7807 com um dicionário
`errors`, que o frontend não sabe ler — o visitante veria a mensagem genérica de fallback.
Preservar o shape mantém a mudança invisível para o frontend.

**D-3: limite de corpo por metadata no endpoint, não global no Kestrel.**
`RequestSizeLimitAttribute` aplicado via `.WithMetadata(...)` limita só `/api/contact`. Um limite
global no Kestrel afetaria qualquer endpoint futuro sem que quem o escrevesse percebesse.

**D-4: mensagens de erro em inglês, escritas à mão.**
O texto vai direto para a tela do visitante via `data.error`, e o site é em inglês. As mensagens
default do .NET ("The field Message must be a string with a minimum length of 1 and a maximum
length of 4000.") são compreensíveis mas destoam do tom da UI, então cada atributo recebe
`ErrorMessage` explícita.

**D-5: teste por HTTP real (`WebApplicationFactory`), não unitário sobre o filtro.**
CT-04 (413) é produzido pelo Kestrel antes de qualquer código do endpoint rodar — nenhum teste
unitário do filtro consegue observá-lo. Custa uma dependência nova no projeto de teste
(`Microsoft.AspNetCore.Mvc.Testing`) e uma linha `public partial class Program;` no fim de
`Program.cs` para tornar a classe visível ao host de teste.

**D-6: a fábrica de teste aponta o Firestore para um endereço morto.**
`WebApplicationFactory` usa o content root da API e leria o `appsettings.json` real — cujo
`Firestore:ProjectId` é o **de produção**. A fábrica define `FIRESTORE_EMULATOR_HOST` para uma
porta fechada, então o seed do startup falha, cai no try/catch de `Program.cs:79-92` (AD-003) e a
aplicação sobe mesmo assim. Garantia dura de que a suíte nunca toca dado real, e efeito colateral
bem-vindo: **estes testes não exigem o emulador no ar**, ao contrário dos de `FirestoreProjectStoreTests`.

**D-7: uma fábrica por teste, não `IClassFixture` compartilhada.**
O rate limit de `Program.cs:64-70` é global (C-6): 3 requisições por minuto para o endpoint
inteiro. Uma fábrica compartilhada faria o 4º teste receber `503` em vez do status esperado — o
gate ficaria vermelho por um motivo que não é o testado. Cada teste sobe sua própria aplicação e
consome 1 das 3 requisições da sua própria janela. Quando C-6 for corrigido, isto pode simplificar.

## Contratos de dados / API

`POST /api/contact` — o contrato de **sucesso** não muda. Acrescenta-se:

| Situação | Antes | Depois |
|---|---|---|
| campo vazio | `400 { success, error }` | `400 { success, error }` (inalterado) |
| e-mail malformado | `200 { success: true }` — enviava | `400 { success, error }` |
| `Message` de 1 MB | `200` — enviava ao Resend | `400 { success, error }` |
| corpo de 10 MB | `200` — desserializava tudo | `413`, **sem corpo JSON** |

O `413` não tem corpo: `frontend/src/lib/api.ts:69` faz `res.json().catch(() => null)` e cai na
mensagem genérica "Failed to send message. Try again later." — degradação aceitável, e um corpo
de 10 MB não é caminho de usuário legítimo (o formulário nem permite chegar lá).

## Análise de reúso

| O que | Reutilizado de (arquivo:linha) | Adaptação necessária |
|---|---|---|
| Atributos DataAnnotations já presentes | `Models/ContactMessage.cs:7-12` | acrescentar `StringLength`/`EmailAddress` e `ErrorMessage`; os `[Required]` ficam como estão e passam a valer |
| Shape de resposta de erro | `Endpoints/ContactEndpoints.cs:14` | copiado literal para o filtro |
| Estilo de teste (xUnit, nome em português, comentário com o ID do requisito) | `PortfolioApi.Tests/FirestoreProjectStoreTests.cs:24-26` | mesmo padrão, com IDs `CT-NN` |
| Padrão de extension method por área | `Endpoints/*.cs` | filtro novo vai em `Validation/`, não em `Endpoints/` — não é endpoint |

## Cadeia de Verificação de Conhecimento — pontos verificados

Tudo abaixo foi verificado **rodando** um protótipo descartável em
`scratchpad/probe/app` (`dotnet new web` + o filtro proposto), não por memória:

| Dúvida | Fonte que resolveu | Resposta |
|---|---|---|
| Minimal API do .NET 9 executa DataAnnotations sozinha? | protótipo | **Não.** Sem o filtro, corpo `{}` e e-mail inválido passavam com `200` |
| `Validator.TryValidateObject` dentro de `IEndpointFilter` funciona? | protótipo | Sim: `{}` → `400 "The Name field is required. The Email field is required. The Message field is required."` |
| `RequestSizeLimitAttribute` vale como metadata de Minimal API (é tipo de MVC)? | protótipo | **Sim.** Corpo de 1 MB com limite de 32 KB → `413`, sem corpo de resposta |
| `[EmailAddress]` barra tentativa de injeção de cabeçalho (`a@b.com\nBcc: x@y.com`)? | protótipo | Sim → `400`. O Resend recebe JSON, então CRLF já não era vetor; a validação fecha a porta antes |
| O `400` do filtro preserva o shape que o frontend lê? | protótipo | Sim: `{"success":false,"error":"..."}` |
| `WebApplicationFactory` exige `Program` público? | codebase + docs .NET | Sim — top-level statements geram classe `internal`; resolvido com `public partial class Program;` |

---

## Revisão de 2026-08-26 — o que a feature `admin-panel` mudou neste design

O design acima é de 2026-08-18. Entre ele e a implementação, a feature `admin-panel`
(2026-08-24) construiu boa parte das peças que ele projetava. **Nada aqui foi invalidado, mas
três decisões mudaram de custo e duas de forma.** O que segue tem precedência sobre o texto
acima onde houver conflito.

**D-1 — cumprida por reúso, não por construção.** `Validation/ValidationFilter.cs:12` já existe,
com o shape `{ success, error }` do D-2 já embutido (`ValidationFilter.cs:27`). Esta feature só
o pendura em `/api/contact`. Ganho colateral: `Validation/NotBlankAttribute.cs` também já existe
e é o que faz o CT-01 valer para `"   "` — `[Required]` sozinho aceita string de espaços.

**D-3 — cumprida por reúso.** `ProjectsEndpoints.cs:16,47` já aplica
`RequestSizeLimitAttribute` como metadata por endpoint. Mesmo padrão aqui, com 32 KB.

**D-5 — a dependência já está paga.** `Microsoft.AspNetCore.Mvc.Testing` entrou no projeto de
teste e `public partial class Program;` está em `Program.cs:151`. A pergunta de aprovação que
pausou a sessão de 08-18 não existe mais: a resposta foi dada na prática.

**D-6 — REVOGADA na forma, mantida no fim.** O design mandava a fábrica apontar
`FIRESTORE_EMULATOR_HOST` para uma porta fechada. Não se faz isso: a variável é **global ao
processo** e derrubaria os testes de integração que rodam em paralelo. `AdminApiFactory.cs:43`
resolve o mesmo problema trocando `Firestore:ProjectId` por um id que não é o de produção. Esta
feature reutiliza a fábrica como está. O fim continua garantido: nenhum teste toca dado real.

**D-7 — mantida.** Uma fábrica por teste. O rate limit de `/api/contact` é de 3/min por
aplicação (`Program.cs:93-99`) e não foi corrigido (C-6 segue aberto), então fábrica
compartilhada faria o quarto teste receber `503` por motivo alheio ao que ele mede.

**D-8 (nova): a ausência de chamada ao Resend é observada, não presumida.**
CT-01..CT-03 exigem "NÃO DEVE chamar o Resend". Com `Email:ApiKey` vazio o `EmailService`
degrada em silêncio (`EmailService.cs:25`) e nunca chamaria nada — um teste nesse estado
afirmaria o requisito sem poder falhar. Então a fábrica destes testes **configura** o e-mail e
troca o handler HTTP por um espião, via
`ConfigureHttpClientDefaults(b => b.ConfigurePrimaryHttpMessageHandler(...))`. O espião conta
requisições e devolve `200` canned. Verificado rodando (probe descartável): o cliente sem nome
de `IHttpClientFactory.CreateClient()` é interceptado, com `RequestUri` visível e **zero** tráfego
de rede. Assim CT-05 prova que o caminho feliz chega ao Resend e CT-01..CT-03 provam que o
inválido não chega.

**D-9 (nova): CT-04 (413) tem teste de declaração + evidência contra Kestrel real.**
Descoberta da `admin-panel` (L-009): o TestServer do `WebApplicationFactory` **não** aplica
`IHttpMaxRequestBodySizeFeature`. O corpo grande chega inteiro e quem responde é a validação
(`400`), não o servidor (`413`). O protótipo de 08-18 que viu `413` rodava contra Kestrel de
verdade, e por isso não conflita com isto. Consequência: o teste automatizado afirma que o
endpoint **declara** o teto de 32 KB, e a evidência de que o servidor honra a declaração é uma
execução real registrada em `evidencias/`. Mesmo tratamento que ADM-10 recebeu.

**Ainda vale sem mudança:** D-2 (shape de erro) e D-4 (mensagens em inglês, escritas à mão).
