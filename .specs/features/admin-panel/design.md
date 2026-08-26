# Design — Painel administrativo

## Decisões arquiteturais

**D-1: JWT stateless, não sessão em memória.**
A instância do Render dorme após 15 min sem tráfego (`PROJECT.md` → Restrições) e sobe zerada.
Uma sessão guardada em memória morreria a cada religamento — na prática, novo login a cada uso.
O JWT carrega o próprio estado, então sobrevive ao restart sem banco de sessão (que custaria
leitura/escrita no Firestore a cada requisição).
**Custo:** não existe logout do lado do servidor — um token vazado vale até expirar. Mitigado por
expiração de 8h e por haver um único usuário; revogar de emergência = trocar `Admin__JwtSigningKey`
no Render, o que invalida todos os tokens de uma vez.

**D-2: a senha vive como hash PBKDF2 em variável de ambiente, nunca em texto puro.**
Formato guardado: `{iterações}.{salt-base64}.{hash-base64}`, PBKDF2-HMAC-SHA256, 210 000 iterações,
salt de 16 bytes, saída de 32 bytes. Comparação com `CryptographicOperations.FixedTimeEquals` —
comparação de string comum vaza o tamanho do prefixo correto por tempo.
**Custo:** cada login gasta ~100 ms de CPU deliberadamente. Num endpoint chamado por uma pessoa
algumas vezes por semana isso é irrelevante, e é justamente o que torna força bruta cara.

**D-3: o hash é gerado pelo próprio binário da API — `dotnet run -- hash-password <senha>`.**
Sem um caminho reproduzível dentro do projeto, a única saída seria colar a senha num gerador
online. O guard fica antes de `WebApplication.CreateBuilder` no `Program.cs`, imprime o valor e
encerra com `return`.
**Custo:** a Fase 1 do `Program.cs` ganha um bloco que não é servidor web — pago para que a
credencial nunca precise sair da máquina.

**D-4: falta de configuração fecha, não abre.**
`AdminOptions.IsConfigured` é falso quando `PasswordHash` ou `JwtSigningKey` estão vazios; nesse
estado o login responde `401` sempre e nenhum token valida (a chave de assinatura registrada é
gerada aleatoriamente no startup, então nada assinado por ninguém é aceito). É o **oposto**
deliberado do `EmailService`, que degrada em silêncio reportando sucesso sem enviar (C-9): ali o
pior caso é um e-mail perdido, aqui seria escrita anônima no banco.

**D-5: `ValidationFilter<T>` genérico com DataAnnotations — desenho herdado do C-5.**
`features/contact-input-validation/design.md` → D-1 já havia projetado e **verificado por
protótipo** que Minimal API do .NET 9 não executa DataAnnotations sozinha. Esta feature constrói
esse filtro e o aplica **apenas** aos endpoints novos; `/api/contact` não é tocado (disciplina de
escopo). Consequência combinada: quando o C-5 for retomado, `Validation/ValidationFilter.cs`,
`Microsoft.AspNetCore.Mvc.Testing` e `public partial class Program;` já existirão — ele fica
reduzido a anotar `ContactMessage` e pendurar o filtro.

**D-6: validação de item de coleção exige atributo próprio (`[ValidateItems]`).**
`Validator.TryValidateObject` **não** desce em objetos aninhados nem em itens de array — verificado
rodando (V1 abaixo): um projeto com decisão de título vazio e tag de 100 caracteres passou como
válido. Sem esse atributo, ADM-10 ficaria atendido só para os campos de primeiro nível.

**D-7: `PUT` é ler-e-então-gravar.**
`SetAsync` sobrescreve o documento inteiro (verificado, F3: os campos ausentes no objeto voltam a
vazio/nulo). Para preservar `createdAt` (ADM-07) e distinguir id inexistente (ADM-08), o endpoint
lê o documento antes, copia o `createdAt` original para o objeto novo e grava.
**Custo:** duas idas ao Firestore por edição. A alternativa — `UpdateAsync` com
`Precondition.MustExist` — resolveria o 404 numa chamada só, mas não responde "qual é o `createdAt`
atual", e o documento é editado algumas vezes por mês.

**D-8: `DELETE` usa `Precondition.MustExist` e traduz a exceção dentro do store.**
`DeleteAsync` em documento inexistente **não lança** (verificado, F2) — sem precondição, ADM-08
responderia `204` para um id inventado. `Precondition.MustExist` devolve `RpcException` com
`StatusCode=NotFound` (F4); o `FirestoreProjectStore` captura e devolve `bool`, para que gRPC não
vaze para a camada de endpoint (`ARCHITECTURE.md`: o store é o único lugar que fala Firestore).

**D-9: id inválido é tratado como inexistente.**
`db.Collection(x).Document("a/b")` lança `ArgumentException` (verificado, F5) — um `%2F` na URL
chegaria ao handler e viraria `500`. Os métodos de escrita novos rejeitam id com `/` devolvendo
"não existe" → `404`.
**Nota de escopo:** `GetByIdAsync` (`Data/FirestoreProjectStore.cs:30`) tem hoje a mesma lacuna e
**não é corrigido aqui** — vira `C-13` em `codebase/CONCERNS.md`.

**D-10: política de rate limit própria para o login, com `429` só nela.**
`RateLimiterOptions.RejectionStatusCode` é global e hoje vale `503` para a política `contact`.
Uma classe `LoginRateLimitPolicy : IRateLimiterPolicy<string>` traz o próprio `OnRejected`, o que
dá `429` no login sem alterar o comportamento de `/api/contact` (verificado, #8).
A partição é **global**, não por IP: atrás do proxy do Render o IP do cliente só chega via
`X-Forwarded-For`, cabeçalho que o próprio atacante controla — particionar por ele seria falsa
sensação de segurança. **Custo:** herda o C-6 conscientemente; alguém pode esgotar a janela e me
deixar 1 minuto sem conseguir entrar. DoS barato de executar e de sobreviver.

**D-11: limite de corpo por endpoint (64 KB), não global.** Mesmo padrão do C-5 → D-3.
`RequestSizeLimitAttribute` via `.WithMetadata(...)` (verificado, #7: corpo de 100 KB → `413`).

**D-12: o token vive em `sessionStorage`, não em cookie `httpOnly`.**
O painel está em `vercel.app` e a API em `onrender.com` — sites diferentes, então o cookie de
sessão seria *third-party*: exigiria `SameSite=None; Secure` e ainda assim é barrado pelo ITP do
Safari e pelas proteções de rastreamento do Chrome. O login funcionaria num navegador e falharia
noutro, sem erro legível.
**Custo:** token legível por JavaScript, portanto exposto a XSS. Mitigação real: `/admin` só
renderiza texto que o próprio admin digitou, não há `dangerouslySetInnerHTML` em `src/`
(verificado por grep) e o token expira em 8h e ao fechar a aba.

**D-13: cliente de escrita separado em `src/lib/admin.ts`.**
`src/lib/api.ts` é o boundary de **leitura server-side com ISR de 60s**; escrita autenticada não
pode passar por cache nem herdar o retry de cold start, que reenviaria um `POST`. O módulo novo é
`"use client"`-safe, usa `cache: "no-store"` e injeta `Authorization`.

**D-14: no painel, erro aparece — o silêncio de `getProjects` não se repete.**
`admin.ts` devolve um resultado discriminado (`{ ok: true, data }` / `{ ok: false, status, error }`).
Engolir a falha faz sentido na vitrine, onde o pior caso é seção vazia; aqui o pior caso é o
usuário achar que salvou e não ter salvo (ADM-22).

**D-15: três camadas de teste, e uma delas custa dependências novas.**
| Camada | Onde | Precisa do emulador? |
|---|---|---|
| Auth e validação por HTTP real | `PortfolioApi.Tests/AdminAuthTests.cs` via `WebApplicationFactory` com `FIRESTORE_EMULATOR_HOST` apontado para porta fechada (C-5 → D-6) | não |
| Escrita ponta a ponta (`POST`/`PUT`/`DELETE`) | `PortfolioApi.Tests/AdminProjectsEndpointTests.cs` sobre o `FirestoreFixture` existente, coleção com GUID | sim |
| Painel | `frontend/src/lib/__tests__/admin.test.ts` (node, `fetch` mockado, no molde de `api.test.ts`) e `frontend/src/components/admin/__tests__/*.test.tsx` (jsdom) | não |

Dependências novas: `Microsoft.AspNetCore.Mvc.Testing` (projeto de teste da API) e
`jsdom` + `@testing-library/react` + `@testing-library/user-event` (devDependencies do frontend).
**Nenhuma entra no bundle do visitante.** Verificado (#10 abaixo) que o jsdom é ligado por arquivo
com o docblock `// @vitest-environment jsdom`, então **não é preciso criar `vitest.config.ts`** e
os testes existentes continuam em ambiente node.

**D-16: a fábrica de teste troca o `ProjectId`, não a variável de ambiente.**
Emenda ao D-6 herdado do C-5, decidida durante a T2. Aquele design apontava
`FIRESTORE_EMULATOR_HOST` para uma porta fechada; a variável é **global ao processo** e o xUnit
roda classes de teste em paralelo, então isso derrubaria `FirestoreProjectStoreTests` rodando ao
mesmo tempo. A fábrica sobrescreve `Firestore:ProjectId` para `demo-portfolio-http` e limpa
`Firestore:CredentialsJson`, o que garante o mesmo que o D-6 queria — nenhum teste alcança o
banco de produção — sem tocar em estado global.

## Contratos de dados / API

Endpoints novos (todos sob `Authorization: Bearer <jwt>`, exceto o login):

| Método | Rota | Corpo | Sucesso | Erros |
|---|---|---|---|---|
| `POST` | `/api/admin/login` | `{ "password": "..." }` | `200 { token, expiresAt }` | `401` senha errada ou admin não configurado · `429` acima de 5/min |
| `POST` | `/api/projects` | `Project` | `201` + `Location` + projeto criado | `400` inválido · `401` sem token · `413` corpo > 64 KB |
| `PUT` | `/api/projects/{id}` | `Project` | `200` + projeto atualizado | `400` · `401` · `404` id inexistente · `413` |
| `DELETE` | `/api/projects/{id}` | — | `204` | `401` · `404` |

`GET /api/projects` e `GET /api/projects/{id}` **não mudam** (ADM-12): seguem anônimos e com o
mesmo shape.

**Campos ignorados na entrada.** Como não há DTO (C-4, `CONVENTIONS.md`), o corpo desserializa em
`Project` e o cliente *pode* mandar `id` e `createdAt`. O servidor os descarta: `id` vem da rota
(ou é gerado pelo Firestore), e `createdAt` é `DateTime.UtcNow` no `POST` e o valor já gravado no
`PUT`. Está aqui porque o compilador não expressa isso — é comportamento que só a spec e o teste
seguram.

**Erro segue o shape da casa:** `{ success: false, error: "<texto>" }`, o mesmo de
`Endpoints/ContactEndpoints.cs:14` e o que o `admin.ts` sabe ler.

## Análise de reúso

| O que | Reutilizado de (arquivo:linha) | Adaptação necessária |
|---|---|---|
| Desenho do `ValidationFilter<T>` | `.specs/features/contact-input-validation/design.md` → D-1 | implementado agora, aplicado só aos endpoints novos |
| Fábrica de teste com Firestore apontado para porta morta | mesmo arquivo → D-6 | idem, para os testes de auth |
| `static class` + `Map*Endpoints` + `MapGroup`/`WithTags` | `Endpoints/ProjectsEndpoints.cs:7-10` | grupo novo `/api/admin`; escrita entra no grupo `/api/projects` existente |
| Shape `{ success, error }` | `Endpoints/ContactEndpoints.cs:14` | copiado literal |
| Coleção com sufixo GUID, parallel-safe | `PortfolioApi.Tests/FirestoreFixture.cs:36-44` | usado como está |
| Estilo de teste (xUnit, nome em português, ID do requisito no comentário) | `PortfolioApi.Tests/FirestoreProjectStoreTests.cs:24-26` | IDs `ADM-NN` |
| `fetch` mockado com `vi.fn`, resposta fake tipada | `frontend/src/lib/__tests__/api.test.ts:11-13` | sem fake timers (não há retry na escrita) |
| Tokens de cor, `.shell`/`.measure`, papéis de fonte | `frontend/src/app/globals.css` (AD-007, AD-009) | painel usa as mesmas utilities; `--clay` **só** em erro, para não quebrar a reserva |
| Padrão de formulário controlado com estado de envio/erro | `frontend/src/components/ContactForm.tsx` | mesma estrutura, com o token no header |

## Cadeia de Verificação de Conhecimento — pontos verificados

Tudo abaixo foi verificado **rodando** protótipos descartáveis em `scratchpad/probe/`
(`dotnet new web` + JwtBearer, console + Firestore contra o emulador, vitest + RTL), nunca por
memória. A saída real está em `evidencias/20260824-cadeia-verificacao.md`.

| # | Dúvida | Fonte | Resposta |
|---|---|---|---|
| 1 | `.RequireAuthorization()` num `MapGroup` de Minimal API devolve `401` ou `404` sem token? | protótipo | `401`, com `WWW-Authenticate: Bearer` |
| 2 | Token malformado, expirado ou com assinatura adulterada é recusado? | protótipo | `401` nos três casos — com `ClockSkew = TimeSpan.Zero`, senão o expirado passa por até 5 min |
| 3 | Dá para emitir JWT sem o pacote `System.IdentityModel.Tokens.Jwt`? | protótipo | Sim: `JsonWebTokenHandler.CreateToken` vem em `Microsoft.IdentityModel.JsonWebTokens`, trazido pelo `JwtBearer` |
| 4 | `Microsoft.AspNetCore.Authentication.JwtBearer` 9.0.16 casa com o `net9.0` do projeto? | `dotnet add package` | Sim, restaurou e compilou |
| 5 | `Rfc2898DeriveBytes.Pbkdf2` estático + `FixedTimeEquals` fazem o ciclo hash/verificação? | protótipo | Sim, `match=true` sobre `salt.hash` em base64 |
| 6 | Dá `429` só no login sem mudar o `503` da política `contact`? | protótipo | Sim, via `IRateLimiterPolicy<string>` com `OnRejected` próprio: `200 200 200 429 429 429` |
| 7 | `RequestSizeLimitAttribute` vale como metadata em Minimal API? | protótipo | Sim: 100 KB com limite de 64 KB → `413` |
| F1 | `Document(id).SetAsync(obj)` funciona com `[FirestoreDocumentId]` preenchido? | emulador | Sim; o campo não é gravado e a leitura repõe o id do documento |
| F2 | `DeleteAsync` em documento inexistente lança? | emulador | **Não** — devolve `UpdateTime` normalmente. Daí o D-8 |
| F3 | `SetAsync` mescla ou sobrescreve? | emulador | Sobrescreve tudo: `tags`/`decisions` viraram vazios e `createdAt` foi substituído. Daí o D-7 |
| F4 | `Precondition.MustExist` dá o quê em id inexistente? | emulador | `RpcException` com `StatusCode=NotFound` |
| F5 | `Document("a/b")` é aceito? | emulador | Lança `ArgumentException: Path must have an odd number of elements`. Daí o D-9 |
| F6 | A ordem de um array de mapas sobrevive ao round-trip? | emulador | Sim: `d1,d2,d3` na mesma ordem (ADM-11) |
| V1 | `Validator.TryValidateObject` desce em item de coleção? | protótipo | **Não.** Decisão com título vazio e tag de 100 chars passaram como válidas |
| V2 | Um `ValidationAttribute` próprio resolve? | protótipo | Sim: `[ValidateItems]` acusou os dois erros, e o payload válido seguiu válido |
| 10 | `@testing-library/react` 16 roda com React 19 no vitest 3, sem `vitest.config.ts`? | protótipo | Sim, com o docblock `// @vitest-environment jsdom` por arquivo; os testes node não são afetados |
| 11 | O `jsx` do tsconfig do frontend serve para compilar `.tsx` de teste? | codebase + protótipo | Sim — `frontend/tsconfig.json:14` é `"jsx": "react-jsx"`. Com `"preserve"` o teste `.tsx` falha (testado) |
| 12 | O CORS atual deixa passar `Authorization`, `PUT` e `DELETE`? | codebase | Sim: `Program.cs:21-23` usa `.AllowAnyHeader().AllowAnyMethod()` |
| 13 | `robots: { index: false }` existe no metadata do Next 16? | codebase | Sim, `next/dist/lib/metadata/types/metadata-interface.d.ts:205` |
