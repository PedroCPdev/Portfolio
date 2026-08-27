# JOURNAL.md

Histórico append-only de mudanças. Entrada mais recente no topo.
Entradas antigas nunca são reescritas nem apagadas.

---

## 2026-08-27 — Painel admin em produção, e limpeza do zip de rascunho

Os 13 commits da `admin-panel` e os 2 do rate limit de contato saíram da máquina e foram para
produção. `/admin` responde, o login com a senha real devolve token e o cadastro de projeto pela
tela funciona — confirmado pelo usuário. Junto foi o `files.zip` que estava sobrando na árvore.

| Arquivo | Ação | Por quê |
|---|---|---|
| `frontend/assets/files.zip` | removido | rascunho de favicon que entrou por engano no `974d1df`; sem referência em `src/`, `next.config` ou `package.json` — verificado por grep antes de remover. A pasta `frontend/assets/` sumiu junto, era o único arquivo |

**Como funciona — sessão do admin, agora em operação**

O mecanismo já está descrito no presente em `codebase/INTEGRATIONS.md` e na entrada de 08-24/26.
O que muda aqui é só o estado: as duas variáveis que faltavam existem no Render, então
`AdminOptions.IsConfigured` passou a ser verdadeiro e a área abriu. Antes deste deploy o código
do admin nem estava no `origin/main` — as variáveis, sozinhas, não faziam nada.

**Evidência de produção (saída real, medida daqui):**
```
GET https://pedrocpdev.vercel.app/admin  → 200
    <meta name="robots" content="noindex, nofollow, nocache">
GET https://pedrocpdev.vercel.app/       → 200, 0 ocorrências de "/admin" no HTML
```
Os dois invariantes que o `design-system.test.ts` guarda continuam valendo fora do teste: a rota
não é indexável e nada público linka para ela. O login em si foi confirmado pelo usuário na tela
— não é verificável daqui, porque exige a senha.

**Não toquei:** o histórico do `974d1df` — o blob de 28 KB continua lá, e reescrever 16 commits
por causa dele não se paga. O `.gitignore` também não mudou: a pasta deixou de existir, não há o
que ignorar. E a revalidação sob demanda do ISR segue Ideia Adiada — uma alteração salva no
painel continua aparecendo na home dentro da janela de 60s, não na hora.

**Ressalva de segurança registrada:** durante a configuração eu imprimi no chat um hash de
exemplo (senha literal `exemplo-descartavel`) e uma chave de assinatura, ambos em texto puro. O
usuário foi avisado de que qualquer um dos dois, se colado no Render, é segredo publicado. Vale
como convenção daqui em diante: exemplo de segredo em conversa nasce queimado, e o comando
`dotnet run -- hash-password` existe justamente para a senha não passar por lugar nenhum.

**Gate `full` (saída real, emulador no ar, antes do envio):**
```
dotnet test PortfolioApi.Tests → Failed: 0, Passed: 94, Skipped: 0, Total: 94
npm test                        → 7 arquivos, 88 testes, 88 passed
npm run build                   → exit 0, /admin na lista de rotas, 3 páginas SSG
npm run lint                    → exit 0
```

**Commit:** `41cfa76`. O envio ao `origin` foi feito pelo usuário — o hook
`block-dangerous-git.sh` barra esse comando partindo do agente, por decisão dele.

## 2026-08-26 — Rate limit do contato por visitante (C-6)

Três requisições de um script deixaram de derrubar o formulário de contato para todo mundo: a
janela de 3/min passou a ser de cada visitante, com um teto de 20/min para o endpoint inteiro, e
o excedente responde `429` em vez de `503`.

| Arquivo | Ação | Por quê |
|---|---|---|
| `PortfolioApi/RateLimiting/ContactRateLimiter.cs` | criado | as duas camadas encadeadas num lugar só; a ordem do encadeamento é a decisão inteira e precisa de um arquivo onde ela caiba explicada |
| `PortfolioApi/Program.cs:93-110` | modificado | `ForwardedHeaders` configurado — sem isso o `RemoteIpAddress` é o do proxy do Render e a partição por IP não existe |
| `PortfolioApi/Program.cs:112-125` | modificado | sai o `AddFixedWindowLimiter("contact")` de partição única; entram o `RejectionStatusCode` 429 e o encadeado como `GlobalLimiter` |
| `PortfolioApi/Program.cs:159-161` | modificado | `UseForwardedHeaders` como primeiro middleware: o limitador lê o IP, e log de IP antes disso registraria o proxy |
| `PortfolioApi/Endpoints/ContactEndpoints.cs:15,33,43` | modificado | a rota vira constante (o limitador reconhece a requisição por ela) e o `.RequireRateLimiting` sai, substituído por comentário apontando para onde o limite passou a morar |
| `PortfolioApi/Auth/LoginRateLimitPolicy.cs:6-19` | modificado | só o XML doc: ele afirmava que particionar por `X-Forwarded-For` é falso isolamento, o que esta feature mediu ser verdade só para o endereço da esquerda |
| `PortfolioApi.Tests/ContactRateLimitTests.cs` | criado | 8 testes cobrindo RL-01..RL-07, incluindo o que prova que recusa por visitante não drena o teto |
| `PortfolioApi.Tests/ContactApiFactory.cs` | criado | `ContactApiFactory` + `ResendSpy` saíram de dentro de `ContactEndpointTests` para serem usados pelas duas classes sem duplicar configuração de teste |
| `PortfolioApi.Tests/ContactEndpointTests.cs` | modificado | docblock (dizia "C-6 segue aberto" e `503`) e a extração acima; nenhuma asserção mudou |
| `.specs/features/contact-rate-limit-por-visitante/spec.md` · `design.md` | criado | RL-01..RL-07 com matriz preenchida, e o design com o porquê de cada ordem testada |
| `.specs/features/contact-rate-limit-por-visitante/evidencias/` (2 arquivos) | criado | cadeia de verificação (12 probes) e o gate `full` |
| `.specs/codebase/CONCERNS.md` | modificado | C-6 marcado RESOLVIDO, ponteiro do backlog para o C-7, e C-10 corrigido — ele descrevia o rate limit como barreira que paga com disponibilidade |
| `.specs/codebase/INTEGRATIONS.md` | modificado | o mecanismo em operação, no presente: gatilho, ordem, sinal de vida, onde mexer |
| `.specs/codebase/ARCHITECTURE.md:11-12,49-51,58-60` | modificado | o diagrama dizia "rate limit 3/min" e a FASE 1 não citava ForwardedHeaders |
| `.specs/codebase/TESTING.md:13` | modificado | contagem verdadeira: 174 → 182 |
| `.specs/project/STATE.md` | modificado | AD-015, L-013, L-014 e três ideias adiadas |
| `.specs/project/ROADMAP.md` | modificado | item 3 do backlog concluído |
| `CLAUDE.md:58` | modificado | dizia "rate-limited to 3 requests/min", que era exatamente o C-6 |
| `.specs/HANDOFF.md` | modificado | próximo item é o C-7 |

**Como funciona — rate limit do formulário de contato**

*Nenhum pré-requisito fora do repositório: os números vivem no código, não em variável de ambiente.*

```
requisição → UseForwardedHeaders (RemoteIpAddress := IP mais à direita do X-Forwarded-For)
           → GlobalLimiter encadeado:
               1. janela do visitante  3/min por IP   → estourou? 429, e para aqui
               2. teto do endpoint    20/min somando todos → estourou? 429
           → endpoint (só /api/contact entra; o resto recebe NoLimiter)
```

1. **Dispara** em toda requisição que chega na API — o limitador é `GlobalLimiter`
   (`Program.cs:121`) e roda antes dos endpoints. Ele próprio decide o que lhe interessa:
   `EhOContato` (`ContactRateLimiter.cs:66`) devolve `NoLimiter` para `/health`, `/api/projects`
   e o login, que passam direto.
2. A ordem importa e não é intuitiva: com o teto adquirido primeiro, uma requisição recusada
   pela janela do visitante consome **2** permits do teto (medido, L-013) — um atacante sozinho
   esgotaria o teto e reproduziria o C-6. Encadeando à mão, o teto só é consultado por quem
   passou pela própria janela.
3. **Sinal de vida:** quatro envios do mesmo navegador em menos de um minuto — o quarto volta
   `429`. Sem `X-Forwarded-For` válido todos caem numa partição só e o limite continua valendo,
   degradando para o comportamento anterior ao C-6, nunca para "sem limite".
4. **Onde mexer:** `PorVisitantePorMinuto` e `TetoDoEndpointPorMinuto`
   (`ContactRateLimiter.cs:26,34`). Para desligar, remover `options.GlobalLimiter`.

**O que não cobre:** spam bem-formado dentro da cota — 3 mensagens/min por IP saturam a cota
diária do Resend sem esbarrar no limite (C-10, próximo do backlog depois do C-7). Não emite
`Retry-After`. E não alcança o login, que segue com partição única.

**Não toquei:** `LoginRateLimitPolicy` além do comentário — particionar o login por IP usaria a
mesma chave e é tentador, mas o C-6 é sobre o endpoint de contato; virou ideia adiada, junto com
a remoção do `OnRejected` que ficou redundante. O frontend não mudou: o `429` cai no mesmo
caminho de erro que o `503` caía. E `ContactEndpointTests` manteve uma fábrica por teste — com
todos os testes na mesma partição do TestServer, a precaução continua necessária.

**Gate `full` (saída real, emulador no ar):**
```
dotnet test PortfolioApi.Tests → Failed: 0, Passed: 94, Skipped: 0, Total: 94
npm test                        → 7 arquivos, 88 testes, 88 passed
npm run build                   → exit 0, 4 rotas, 3 páginas SSG
npm run lint                    → exit 0
```
RED antes da implementação: `Failed: 7, Passed: 1` — o único verde era `/health`, que é guarda de
regressão do novo `GlobalLimiter`, não requisito dirigido por RED.

**Commit:** `feat(api): limitar contato por visitante em vez de globalmente`

---

## 2026-08-26 — Validação de entrada no POST /api/contact (C-5)

O formulário de contato deixou de aceitar qualquer coisa: nome e mensagem sem teto, e-mail em
qualquer formato e corpo de até 30 MB agora são recusados com `400` antes de virar e-mail, e o
corpo tem limite de 32 KB imposto pelo servidor.

| Arquivo | Ação | Por quê |
|---|---|---|
| `PortfolioApi/Models/ContactMessage.cs:18-32` | modificado | os limites passam a viver onde se lê o contrato: `[NotBlank]`, `[StringLength]` 80/254/4000 e `[EmailAddress]`, com mensagem em inglês porque o texto vai para a tela do visitante |
| `PortfolioApi/Endpoints/ContactEndpoints.cs:42-43` | modificado | pendura o `ValidationFilter<ContactMessage>` e declara o teto de 32 KB — sem o filtro, atributo no model é decoração |
| `PortfolioApi/Endpoints/ContactEndpoints.cs:20-25` | modificado | sai a checagem manual de `IsNullOrWhiteSpace` do handler: atrás do filtro ela virou código morto |
| `PortfolioApi.Tests/ContactEndpointTests.cs` | criado | 15 testes de CT-01..CT-05 por HTTP, com espião no lugar do Resend |
| `.specs/features/contact-input-validation/spec.md` | modificado | matriz de rastreabilidade preenchida com `arquivo:linha`, 5 de 5 verificados |
| `.specs/features/contact-input-validation/design.md` | modificado | revisão de 08-26: o que a `admin-panel` já tinha construído, D-6 revogada na forma, D-8 e D-9 novas |
| `.specs/features/contact-input-validation/evidencias/20260826-limite-de-corpo-contato-kestrel.md` | criado | o `413` não é observável em TestServer; a prova é execução contra Kestrel real |
| `.specs/codebase/CONCERNS.md` | modificado | C-5 marcado RESOLVIDO e o ponteiro do backlog movido para o C-6 |
| `.specs/codebase/ARCHITECTURE.md:11-12` | modificado | o fluxo do contato passa pelo filtro; o diagrama dizia que ia direto ao EmailService |
| `.specs/codebase/TESTING.md:13` | modificado | contagem verdadeira: 159 → 174 |
| `.specs/project/ROADMAP.md` · `STATE.md` | modificado | item 2 do backlog concluído; L-011 e L-012 |
| `.specs/HANDOFF.md` | modificado | próximo item é o C-6, com a nota de que os testes de contato dependem da janela atual |
| `CLAUDE.md:58` | modificado | descrevia o endpoint como "validates, sends" sem dizer o que valida |

**Como funciona — validação do corpo do contato**

```
POST /api/contact → Kestrel (teto de 32 KB) → rate limiter (3/min) → ValidationFilter → handler → Resend
```

1. **Dispara** a cada `POST /api/contact` — sob demanda, sem agendamento. O teto de corpo é
   declarado em `ContactEndpoints.cs:16,43` e o filtro em `:42`.
2. O Kestrel corta o corpo acima de 32 KB antes de desserializar (`413`, sem corpo de resposta).
   Passando daí, o `ValidationFilter<ContactMessage>` (`Validation/ValidationFilter.cs:20`) roda
   as DataAnnotations do model e, em falha, devolve `400 { success, error }` — o mesmo shape que
   `frontend/src/lib/api.ts` já lê para mostrar o erro ao visitante. Só então o handler roda,
   loga e chama o Resend.
3. **Está vivo** se um `POST` com e-mail malformado devolve `400` com `"Email must be a valid
   email address."` no campo `error`. No log da API, requisição recusada **não** produz linha
   `[Contact] From: ...` — a ausência dessa linha é o sinal de que a barreira agiu antes do handler.
4. **Para mexer ou desligar:** os limites são os atributos em `ContactMessage.cs:18-32`; o teto de
   corpo é a constante em `ContactEndpoints.cs:16`. Remover `.AddEndpointFilter` de `:42` desliga
   a validação inteira e devolve o endpoint ao estado do C-5 (12 dos 15 testes ficam vermelhos).

**Não cobre:** spam bem-formado (C-10 segue aberto — validar formato não impede robô educado);
o `reply_to` continua podendo declarar o e-mail de outra pessoa; e o rate limit segue global,
não por visitante (C-6, próximo item). O formulário do frontend **não** ganhou `maxLength`: quem
digitar 5000 caracteres só descobre no envio.

**O teste que quase não podia falhar.** CT-01..CT-03 exigem "não deve chamar o Resend". Com
`Email:ApiKey` vazio — que é como as fábricas de teste já subiam a aplicação — o `EmailService`
degrada em silêncio e nunca chamaria nada: a asserção passaria verde para sempre, inclusive com
a validação removida. A correção foi configurar o e-mail no teste e trocar o handler HTTP por um
espião que conta chamadas e não toca a rede. Virou L-011 e L-012 em `STATE.md`.

**Por que este item saiu barato.** A spec e o design são de 2026-08-18 e previam construir o
`ValidationFilter<T>`, adotar `Microsoft.AspNetCore.Mvc.Testing` e tornar `Program` público. A
feature `admin-panel` (08-24) já tinha feito as três coisas por conta própria — aqui foi só
reúso. A pergunta de aprovação que pausou a sessão de 08-18 morreu por obsolescência. Uma
decisão daquele design foi **revogada**: apontar `FIRESTORE_EMULATOR_HOST` para porta morta
derrubaria os testes de integração em paralelo, porque a variável é global ao processo.

**Não toquei:** o `maxLength` dos inputs de `ContactForm.tsx` — é UX, está fora de escopo
declarado na spec, e a barreira que importa é a do servidor. O rate limit (C-6) e o anti-spam
(C-10), que são itens próprios do backlog. E `ContactMessage` segue sem DTO, conforme C-4.

**Gate `quick` (saída real, com emulador no ar):**
`dotnet build` 0 warnings/0 errors · `dotnet test` **86 passed, 0 skipped** (era 71).
Frontend rodado por conferência, intocado: `npm test` 88 passed. Total 174.
Mutação de controle: removendo `.AddEndpointFilter`, 12 dos 15 testes novos ficam vermelhos.

## 2026-08-24/26 — Painel administrativo autenticado (feature `admin-panel`)

Cadastrar, editar e excluir projeto deixou de ser digitação manual no console do Firebase e
passou a ser uma tela em `/admin`, protegida por senha; a API ganhou escrita autenticada por JWT
em `/api/projects`, com validação declarativa do corpo e limite de tamanho.

| Arquivo | Ação | Por quê |
|---|---|---|
| `PortfolioApi/Auth/AdminOptions.cs` | criado | concentra a configuração do admin e o `IsConfigured` que fecha a área quando falta variável |
| `PortfolioApi/Auth/AdminPasswordHasher.cs` | criado | senha nunca em claro na configuração: PBKDF2 `210000.<salt>.<hash>` |
| `PortfolioApi/Auth/AdminTokenService.cs` | criado | emissão e parâmetros de validação do JWT num lugar só, para os dois lados não divergirem |
| `PortfolioApi/Auth/LoginRateLimitPolicy.cs` | criado | login é o único endpoint que aceita adivinhação; 5/min corta força bruta |
| `PortfolioApi/Endpoints/AdminEndpoints.cs` | criado | `POST /api/admin/login`, seguindo a convenção `Map*Endpoints` dos outros grupos |
| `PortfolioApi/Endpoints/ProjectsEndpoints.cs` | modificado | `POST`/`PUT`/`DELETE` autenticados; `GET` permanece anônimo (ADM-12) |
| `PortfolioApi/Validation/ValidationFilter.cs` | criado | Minimal API não executa DataAnnotations sozinha — sem o filtro, atributo no model é decoração |
| `PortfolioApi/Validation/ValidateItemsAttribute.cs` | criado | o `Validator` não desce nos itens de uma coleção; sem ele, decisão inválida passava |
| `PortfolioApi/Validation/NotBlankAttribute.cs` | criado | `[Required]` aceita string só de espaço, que é título vazio na tela |
| `PortfolioApi/Models/Project.cs` · `Decision.cs` | modificado | limites declarados onde o dado é definido, não espalhados pelo endpoint |
| `PortfolioApi/Models/LoginRequest.cs` | criado | corpo do login, o único payload que não é um model de domínio |
| `PortfolioApi/Data/FirestoreProjectStore.cs` | modificado | `UpdateAsync`/`DeleteAsync`; traduz `RpcException` e id inválido em `false`, para gRPC não subir ao endpoint |
| `PortfolioApi/Program.cs` | modificado | wiring de JwtBearer, rate limit e o guard `hash-password`; `public partial class Program;` para o teste enxergar |
| `PortfolioApi/PortfolioApi.csproj` | modificado | `Microsoft.AspNetCore.Authentication.JwtBearer` 9.0.16 |
| `PortfolioApi/appsettings.Example.json` | modificado | as duas variáveis novas visíveis para quem for configurar o Render |
| `PortfolioApi.Tests/AdminApiFactory.cs` | criado | sobe a app em memória com `FIRESTORE_EMULATOR_HOST` em porta morta — a suíte nunca fala com Firestore de produção |
| `PortfolioApi.Tests/AdminAuthTests.cs` | criado | login: sucesso, fail-closed sem configuração, senha errada, 429 |
| `PortfolioApi.Tests/AdminPasswordHasherTests.cs` | criado | hash malformado devolve `false` em vez de lançar |
| `PortfolioApi.Tests/AdminProjectsEndpointTests.cs` | criado | escrita ponta a ponta: 201/200/404/401, `createdAt` preservado, `id` do cliente ignorado |
| `PortfolioApi.Tests/ProjectValidationTests.cs` | criado | limites e mensagens de erro nomeando o campo |
| `PortfolioApi.Tests/FirestoreProjectStoreTests.cs` | modificado | round-trip de escrita e ordem das decisões contra o emulador |
| `PortfolioApi.Tests/PortfolioApi.Tests.csproj` | modificado | `Microsoft.AspNetCore.Mvc.Testing`, sem a qual não há teste HTTP |
| `frontend/src/lib/admin.ts` | criado | fronteira separada da `api.ts`: `no-store`, `Authorization` e resultado discriminado que expõe o erro |
| `frontend/src/app/admin/page.tsx` · `layout.tsx` | criado | rota do painel e `robots: { index: false }` |
| `frontend/src/components/admin/AdminPanel.tsx` | criado | alterna login/lista/formulário e trata sessão expirada num lugar só |
| `frontend/src/components/admin/LoginForm.tsx` | criado | entrada da sessão, sem revelar se a senha ou a configuração falhou |
| `frontend/src/components/admin/ProjectForm.tsx` | criado | criação e edição na mesma tela, preenchida a partir do projeto atual |
| `frontend/src/components/admin/DecisionEditor.tsx` | criado | a ordem das decisões é conteúdo, então precisa ser editável |
| `frontend/src/components/admin/DeleteConfirm.tsx` | criado | exclusão é irreversível: exige o título digitado igual |
| `frontend/src/components/admin/__tests__/*.test.tsx` (4 arquivos) | criado | comportamento de cada componente, jsdom por docblock |
| `frontend/src/lib/__tests__/admin.test.ts` | criado | contrato do cliente com `fetch` mockado, sem rede |
| `frontend/src/__tests__/design-system.test.ts` | modificado | guarda: `/admin` não pode virar link público nem perder o `robots` |
| `frontend/vitest.config.ts` | criado | resolve o alias `@/`; sem ele o vitest resolve import de tipo, mas não de valor |
| `frontend/package.json` · `package-lock.json` | modificado (lock gerado) | `jsdom`, `@testing-library/react`, `user-event` — dev only, nada entra no bundle |
| `.specs/features/admin-panel/{context,spec,design,tasks}.md` | criado | requisitos ADM-01..ADM-22, decisões D-1..D-15 e a quebra em T1..T13 |
| `.specs/features/admin-panel/evidencias/` (3 arquivos) | criado | cadeia de verificação, limite de corpo no Kestrel real e o gate `full` |
| `.specs/codebase/ARCHITECTURE.md` | modificado | o fluxo do admin e as camadas `Auth/` e `Validation/` no presente |
| `.specs/codebase/CONVENTIONS.md` · `INTEGRATIONS.md` | modificado | convenção de validação e o mecanismo de sessão como documento vivo |
| `.specs/codebase/TESTING.md` | modificado | contagem verdadeira (dizia 50, são 159) e como o jsdom é ligado |
| `.specs/codebase/CONCERNS.md` | modificado | C-13: `GET` com id contendo barra responde 500 — achado, fora do escopo |
| `.specs/project/PROJECT.md` · `ROADMAP.md` · `STATE.md` | modificado | revoga o fora-de-escopo, marca a feature como feita, AD-012..AD-014 e L-008..L-010 |
| `CLAUDE.md` | modificado | descrevia o painel como inexistente e afirmava que não havia test runner |
| `.specs/HANDOFF.md` | modificado | apontava para a sessão de 08-18; agora aponta para o C-5 |

**Como funciona — sessão do admin**

**Pré-requisito fora do repositório: `Admin__PasswordHash` e `Admin__JwtSigningKey` (≥ 32 bytes)
no Render. Sem as duas a área fica fechada — login e escrita respondem 401.**

```
/admin → POST /api/admin/login → JWT 8h em sessionStorage → Authorization: Bearer em toda escrita
```

1. **Dispara** quando alguém abre `/admin` e envia a senha (`components/admin/LoginForm.tsx:15`).
   Não há agendamento: é sob demanda.
2. `AdminEndpoints.cs:33` compara a senha com o hash PBKDF2; batendo, `AdminTokenService.cs:21`
   assina um JWT HS256 de 8h. O painel guarda o token em `sessionStorage` e o lê por
   `useSyncExternalStore` (`lib/admin.ts:91`). Toda escrita manda `Authorization: Bearer`;
   `ValidationFilter<Project>` roda antes do handler e o corpo é limitado a 64 KB.
   Um `401` em qualquer ação apaga a sessão e devolve ao login (`AdminPanel.tsx:48`).
3. **Está vivo** se `POST /api/admin/login` com a senha certa devolve `200` com `token` e
   `expiresAt`. Faltando configuração, devolve `401` mesmo com a senha certa — esse é o sinal de
   variável ausente, não de senha errada.
4. **Para mexer ou desligar:** trocar `Admin__JwtSigningKey` invalida todos os tokens de uma vez
   (é a revogação de emergência). Esvaziar as variáveis fecha a área inteira.
   O `RequireAuthorization()` em `Endpoints/ProjectsEndpoints.cs:45` é o ponto único da proteção.

**Não cobre:** logout do lado do servidor — token vazado vale até expirar. Também não invalida o
cache ISR: uma alteração salva no painel aparece na home dentro da janela de 60s, não na hora.

**Por que uma fronteira nova em vez de estender `api.ts`:** a `api.ts` existe para engolir falha
e devolver `[]`, que é o certo para a home pública e o oposto do que um painel precisa — ali um
erro tem que aparecer na tela. Ver AD-013 e AD-014.

**O que a Cadeia de Verificação evitou:** três suposições morreram rodando código, não por
memória — o TestServer não aplica limite de corpo (L-009, por isso o `413` tem evidência
separada contra Kestrel), o Firestore trunca `DateTime` em microssegundo (L-008) e o lint do
Next 16 reprova `setState` síncrono dentro de `useEffect` (L-010, origem do `useSyncExternalStore`).

**Não toquei:** a correção do `GET /api/projects/{id}` com id inválido — virou C-13 em
`CONCERNS.md`, porque a tarefa era o painel, não o endpoint de leitura. A revalidação sob demanda
do ISR e o texto "No admin area" do `DataSeeder`, que a feature tornou falso — ambos viraram
Ideia Adiada. O `frontend/assets/files.zip`, que entrou no commit `974d1df` por engano e
**segue no repositório**, pendente de decisão (os 12 commits ainda não foram enviados).

**Gate `full` (saída real, revalidado em 2026-08-26 após reboot da máquina):**
`dotnet build` 0 warnings/0 errors · `dotnet test` 71 passed, 0 skipped (emulador no ar) ·
`npm test` 88 passed (7 arquivos) · `npm run build` compilado com `/admin` na lista de rotas ·
`eslint` exit 0. Total **159**, contra baseline de 59 medida antes da T1 — nenhum teste removido,
pulado ou enfraquecido.

**Commits:** `0709a1c`..`fcc41a8` (T1..T12) e `docs(specs): registrar painel admin e decisoes AD-012..AD-014` (T13).

## 2026-08-20 — Ícone do site e posicionamento sem recorte de stack

A página deixou de se apresentar como "backend engineer working in C# and .NET" e passou a se
apresentar como software engineer, sem nomear stack no texto corrido; a aba do navegador deixou
de mostrar o favicon padrão do Next e passou a mostrar o ⟨P⟩.

| Arquivo | Ação | Por quê |
|---|---|---|
| `frontend/src/app/icon.svg` | criado | o brackets ⟨P⟩ escolhido pelo usuário, pela convenção de arquivo do App Router |
| `frontend/src/app/favicon.ico` | removido | era o ícone padrão do Next, intocado desde `c11f5bf`; se ficasse, concorreria com o `icon.svg` no `<head>` |
| `frontend/src/app/layout.tsx:27-29` | modificado | `<title>` e description: "backend engineer ... C# and .NET" → "software engineer" |
| `frontend/src/components/Hero.tsx:8-9,26-29` | modificado | trilho `Backend / C# · .NET` → `Software engineer / APIs · data · deploy`; parágrafo perde o rótulo de stack |
| `frontend/src/components/About.tsx:29-33` | modificado | "I maintain and build .NET services" → "I build and maintain services"; o resto do parágrafo já era neutro |
| `frontend/src/components/Contact.tsx:16-17` | modificado | "Open to backend roles and to collaborating on .NET work" → "in any language or stack" |
| `frontend/src/__tests__/design-system.test.ts` | modificado | +8 casos: `PS-01` (prosa sem recorte, nos dois sentidos) e `PS-02` (ícone) |
| `.specs/features/generic-positioning-and-icon/spec.md` | criado | requisitos, decisões do usuário e rastreabilidade |
| `.specs/project/STATE.md` | modificado | AD-010, AD-011 e duas ideias adiadas |

**O teste que guarda os dois sentidos.** `PS-01` falha se `C#`, `.NET`, `ASP.NET` ou `backend`
voltarem à prosa — e falha também se `.NET` sumir da lista Core do About. Sem a segunda metade,
uma limpeza futura genericizaria a página até ela não afirmar mais nada. A separação virou
AD-010: prosa é posicionamento, lista de tecnologias é fato.

**Achado que mudou a implementação.** O `icon-brackets.svg` do zip pintava tudo com
`var(--bg)`/`var(--fg)`. Rasterizado por librsvg 2.62 (motor SVG do ImageMagick e de vários
crawlers e geradores de preview) ele saía **quadrado preto sólido, sem o P**: custom property
não é resolvida, `fill` e `stroke` viram declaração inválida e são descartados. Verificado por
experimento — três SVGs mínimos rasterizados lado a lado: cor literal ✓, seletor de classe com
`@media` ✓, `var()` ✗. O `icon.svg` commitado usa seletor de classe, geometria idêntica ao
original. Ver AD-011.

**Limitação conhecida, sinalizada antes da escolha e aceita:** a 16px os chevrons ficam macios.
Confirmado rasterizando em 16/32/64 de verdade — o P continua identificável, o ⟨ ⟩ perde
definição. O `icon-monogram.svg` do zip é a alternativa se incomodar no ar.

**Gate (saída real):** `npm test` 39/39 (baseline era 31 — subiu 8, não caiu),
`npm run build` compilado com `/icon.svg` listado como rota, `eslint` exit 0. Verificado também
no artefato construído, não só no teste: o `<head>` de `.next/server/app/index.html` traz
exatamente um `<link rel="icon" ... type="image/svg+xml">`, o `<title>` é o novo, e o texto
extraído das quatro seções não contém `C#`, `.NET` nem `backend`.

**Não toquei:** as duas `description` dos projetos em produção (Firestore) — decisão do usuário;
descrevem projetos que **são** de fato .NET e genericizá-las seria mentir sobre o que o projeto
é. A lista Core/Also do About, que segue sendo o sinal `.NET` mais forte da página (4 de 6),
também por decisão dele — registrada como ideia adiada. O rodapé
(`Next.js frontend · ASP.NET Core API · Firestore`), que descreve a stack do próprio site e é
fato. E `frontend/assets/files.zip`, que segue não rastreado, para o usuário decidir se entra.


## 2026-08-20 — Description do Portfolio Frontend em produção (dado real)

Última das quatro operações em dado real. Um campo, um documento.

| Alvo | Ação | Por quê |
|---|---|---|
| `projects/R9ZGZNOdNpZtYWkYWnTN` (Portfolio Frontend) | `description` reescrita | o texto anterior terminava em "C# API integrated", truncado, e repetia o que as tags já diziam |

Antes: *"Personal Portfolio built using Next.js 16 and React 19. Minimalistic dark design,
C# API integrated."*
Depois: *"The site you are reading. One server-rendered page — no client-side router, no state
library — and every project on it comes from the C# API in the same repository."*

**O critério.** Na vitrine a `description` é o resumo do card, logo acima das tags. Repetir
"Next.js 16 e React 19" ali era gastar duas linhas para dizer o que a linha de tags diz melhor.
O texto novo usa o espaço para o que as tags não conseguem: o que a página **é** e como ela se
liga ao outro projeto. Também espelha a voz do card da API, que fecha com "if you are reading
this, it answered" — os dois agora se apresentam do mesmo jeito.

**Escrita cirúrgica:** `updateMask.fieldPaths=description`. As 5 decisões, as tags, urls e
`createdAt` intactos, confirmado contra o dump.

**Rollback:** `scratchpad/dump-prod-2026-08-20180431.json`.

**Verificado no site público:** o texto novo apareceu ~30s depois e `C# API integrated` sumiu.
Sem redeploy.

**Estado final dos dois documentos de produção:** 5 decisões cada, tags versionadas, descrições
reescritas. Nenhum campo pendente que eu tenha identificado.


## 2026-08-20 — Tags do Portfolio Frontend em produção (dado real)

Terceira e menor operação em dado real: só o campo `tags` de um documento.

| Alvo | Ação | Por quê |
|---|---|---|
| `projects/R9ZGZNOdNpZtYWkYWnTN` (Portfolio Frontend) | `tags` reescritas | `Next.js` e `React` sem versão não dizem se é App Router ou Pages, nem React 18 ou 19 — e o card da API ao lado já estava versionado |

`TypeScript, Next.js, React, Tailwind CSS` → `TypeScript, Next.js 16, React 19, Tailwind v4, Vercel`.

**Por que versionar.** Numa vitrine de portfólio a versão é metade da informação: "Next.js" pode
ser Pages Router de 2021, "Next.js 16" não. `Vercel` entrou para o card espelhar o da API, que
carrega `Render` — o alvo de deploy é parte do que o projeto demonstra.

**Escrita cirúrgica:** `updateMask.fieldPaths=tags`, e só `tags` no corpo. As 5 decisões, a
`description`, as urls e o `createdAt` seguem intactos, confirmado por comparação com o dump.

**Rollback:** `scratchpad/dump-prod-2026-08-20175940.json`.

**Verificado no site público:** as tags novas apareceram na home ~30s depois, e `Tailwind CSS`
sumiu. Sem redeploy — o ISR de 60s virou sozinho.

**Não toquei:** a `description` do Portfolio Frontend, que segue "Personal Portfolio built using
Next.js 16 and React 19. Minimalistic dark design, C# API integrated." O trecho "C# API
integrated" ficou truncado e agora é redundante com as tags, mas a mudança não foi pedida.


## 2026-08-20 — Mais decisões e tags do Portfolio API em produção (dado real)

Segunda operação em dado real. Os dois projetos foram de 3 para 5 decisões, e as tags do
Portfolio API deixaram de anunciar detalhe interno. Nenhum arquivo de aplicação foi tocado.

| Alvo | Ação | Por quê |
|---|---|---|
| `projects/kBo99k1ss9KSRJCK8HrY` (Portfolio API) | +2 decisões (AD-002, AD-004/L-006) e `tags` reescritas | `NoSQL` e `Scalar` são detalhe interno; `Docker`, `Render` e `Minimal API` são o que o projeto demonstra |
| `projects/R9ZGZNOdNpZtYWkYWnTN` (Portfolio Frontend) | +2 decisões (AD-008, AD-009) | as decisões desta sessão não estavam no site |

**Interpretação assumida.** "as que tomamos" foi lido como *as `AD-NNN` do `STATE.md` que ainda
não estavam publicadas* — inclusive as desta sessão. Produção já tinha 3 decisões em cada projeto
quando o pedido chegou, verificado por leitura antes de escrever, então "está sem decisões" não
podia ser literal.

**Acrescenta, não substitui.** O script lê as decisões existentes e escreve
`[...existentes, ...novas]`, preservando ordem. Um `PATCH` com a lista nova sozinha teria apagado
as três anteriores — o array é substituído inteiro, não mesclado, e essa é a armadilha do
`updateMask` em campo de array.

**Rollback:** `scratchpad/dump-prod-2026-08-20175305.json`, tirado imediatamente antes.

**Verificado no site público, não só no banco:** as 5 decisões e as tags novas aparecem em
`pedrocpdev.vercel.app/projects/kBo99k1ss9KSRJCK8HrY`, cerca de 30s após a escrita — o ISR de 60s
virou sozinho, sem redeploy.

**Não toquei:** as tags do Portfolio Frontend (`TypeScript, Next.js, React, Tailwind CSS`), que não
foram pedidas e não têm o mesmo problema; a `description` do Frontend; e o documento do
"Keep-alive scheduler", que segue inexistente em produção.


## 2026-08-20 — Backfill de `decisions` em produção (dado real)

Operação em dado real, autorizada pelo usuário depois de dump e revisão do conteúdo.
Não é mudança de código: nenhum arquivo da aplicação foi tocado nesta entrada.

| Alvo | Ação | Por quê |
|---|---|---|
| `projects/kBo99k1ss9KSRJCK8HrY` (Portfolio API) | `decisions` (3) + `description` reescrita | a description tinha frase quebrada e virou o resumo visível no card da home |
| `projects/R9ZGZNOdNpZtYWkYWnTN` (Portfolio Frontend) | `decisions` (3) | fecha a página de detalhe com conteúdo real |

**Como foi feito, e por que não pelo console.** À mão seriam 18 entradas de texto dentro de uma
estrutura array-de-map, que o console do Firebase trata mal. Foi um script sem dependências
(`scratchpad/write-prod.mjs`) assinando JWT com a service account de `~/.config/gcloud/`.

**O que protegeu a operação:** `updateMask.fieldPaths` em cada PATCH. Sem ele o Firestore
**substitui o documento inteiro** e `title`, `tags`, `githubUrl`, `liveUrl` e `createdAt` teriam
sido apagados. A verificação pós-escrita comparou todos os campos contra o dump: preservados.

**Rollback disponível:** `scratchpad/dump-prod-2026-08-20174041.json` guarda o estado anterior dos
dois documentos. Mesmo sem rollback, apagar `decisions` devolve array vazio na leitura e a página
degrada sem quebrar — é o que o teste PD-02 garante.

**Não toquei:** a `description` do Portfolio Frontend (não foi aprovada; segue "Personal Portfolio
built using Next.js 16 and React 19. Minimalistic dark design, C# API integrated."). Não criei o
documento do "Keep-alive scheduler" — ele nunca existiu em produção, era invenção minha para o
preview local. Nenhuma regra, índice ou outra coleção foi tocada.

**Ordem importa e foi respeitada:** o backfill entrou **antes** do deploy. A API que está no ar
ainda não conhece `decisions` e simplesmente ignora o campo, então nada quebrou no intervalo — e
quando o deploy entrar, as páginas de detalhe já nascem com conteúdo.


## 2026-08-20 — Nome completo no cabeçalho do hero

O hero passou a abrir com "Pedro Chasci Puga" acima da tese. É página de portfólio: o nome de
quem assina precisava estar visível, não só no `<title>` e no handle da navbar.

| Arquivo | Ação | Por quê |
|---|---|---|
| `frontend/src/components/Hero.tsx` | modificado | nome como primeira linha **dentro** do `<h1>`, em display face menor que a tese |
| `frontend/src/__tests__/design-system.test.ts` | modificado | asserção de que o nome completo está dentro do `h1`, não solto acima dele |

**Por que dentro do `h1` e não num elemento próprio:** separar o nome em um `<p>` acima o tiraria
da estrutura de títulos da página. Numa página de portfólio o nome é o cabeçalho principal —
junto com a tese ele forma o `h1`, o que também é o que a busca por "Pedro Chasci Puga" indexa.
O peso visual continua na tese: o nome é bem menor e sem o extrabold.

**Não toquei:** o handle `pedrocpdev` na navbar e no footer continua como está — é o identificador
curto, e repetir o nome completo nos três lugares seria redundante.

**Gate:** `npx vitest run` → 31 passed · `npx eslint .` → exit 0 · `npm run build` → compilou.


## 2026-08-20 — Vitrine na home, raciocínio em `/projects/{id}`, e largura que acompanha a tela

A home voltou a ser vitrine: um card por projeto, em grid que vai de 1 a 3 colunas. O raciocínio
de engenharia saiu dela e ganhou página própria, onde cada decisão aparece com o porquê e o custo.
O campo `tradeoff` (string única) deu lugar a `decisions: Decision[]`.

| Arquivo | Ação | Por quê |
|---|---|---|
| `PortfolioApi/Models/Decision.cs` | criado | `title`/`why`/`cost`; a ordem na lista é informação, não detalhe de armazenamento |
| `PortfolioApi/Models/Project.cs` | modificado | `Decisions` (`:31`) no lugar de `Tradeoff`; default `[]`, nunca null |
| `PortfolioApi/Services/DataSeeder.cs` | modificado | dois projetos de exemplo com duas decisões cada |
| `PortfolioApi.Tests/FirestoreProjectStoreTests.cs` | modificado | round-trip da lista aninhada, ordem, custo vazio, e documento cru sem a chave |
| `PortfolioApi.Tests/ProjectContractTests.cs` | modificado | `decisions[].title/why/cost` em camelCase, array vazio, e ausência de `tradeoff` |
| `frontend/src/lib/api.ts` | modificado | `Decision`, `decisions?`, e `getProject` (`:65`); retry de cold start extraído para servir as duas chamadas |
| `frontend/src/app/projects/[id]/page.tsx` | criado | rota de detalhe, `generateStaticParams`, `generateMetadata`, `notFound()` |
| `frontend/src/components/DecisionRecord.tsx` | renomeado de `Ledger.tsx` | um registro por decisão; `why` e `cost` lado a lado a partir de `lg` |
| `frontend/src/components/Projects.tsx` | modificado | grid de cards `1 → 2 → 3` colunas, cada card linkando para o detalhe |
| `frontend/src/components/Hero.tsx` | modificado | bloco AD-001 removido: decisão não mora mais na home |
| `frontend/src/components/Section.tsx` | modificado | usa `.shell`; título com `clamp` |
| `frontend/src/components/Navbar.tsx` | modificado | `<Link>` no lugar de `<a>` (a nav existe em duas rotas agora), âncoras absolutas `/#work` |
| `frontend/src/app/globals.css` | modificado | `.shell` (`:61`) e `.measure` (`:82`) |
| `frontend/src/app/page.tsx`, `About.tsx`, `Contact.tsx` | modificados | contêiner e medida de leitura compartilhados |
| `frontend/src/__tests__/design-system.test.ts` | modificado | bloco DR-05 substituído pelos PD-01/03/04/05/08 |
| `.specs/features/project-detail-page/` | criado | spec PD-01..PD-09 com matriz |
| `.specs/project/STATE.md` | modificado | AD-008 (separar vitrine de raciocínio) e AD-009 (largura em `.shell`/`.measure`) |
| `.specs/codebase/CONVENTIONS.md`, `TESTING.md`, `CLAUDE.md` | modificados | reserva do clay aponta para o arquivo novo, regra de largura, contagem do gate, e as duas rotas |

**Por que os testes de `tradeoff` sumiram.** Não foram apagados para reduzir falha: o campo deixou
de existir por decisão do usuário (AD-008), e cada um foi substituído pelo equivalente em
`decisions` — inclusive o mais importante, o que grava um documento **cru** sem a chave para provar
que documento antigo não estoura na leitura. A contagem subiu: 18 → 20 na API, 22 → 30 no frontend.

**A queixa de "tudo centralizado" era estrutural.** O layout prendia tudo em `max-w-5xl` (64rem)
repetido em oito arquivos. A correção não foi aumentar o número: foi separar largura de contêiner
(`.shell`) de largura de leitura (`.measure`), porque esticar a segunda junto com a primeira só
troca uma coluna estreita por linha de texto ilegível. Um teste falha se `max-w-5xl` voltar.

**Duas correções vieram do screenshot, não do gate:** as réguas de `Why`/`Cost` esticavam pela
coluna inteira enquanto o texto parava em 65ch, deixando régua pendurada sobre vazio; e o registro
de decisão tinha três níveis de indentação. Ambas invisíveis para build, lint e testes.

**Não toquei:** os documentos de produção seguem sem `decisions`, então em `pedrocpdev.vercel.app`
cada página de detalhe vai dizer que as decisões ainda não foram escritas até você preenchê-las.
Não criei endpoint de escrita nem painel admin. Não mexi em `imageUrl`, que continua no model sem
uso na UI.

**Gate:** `dotnet test PortfolioApi.Tests` → 20 passed (emulador) · `npx vitest run` → 30 passed ·
`npx eslint .` → exit 0 · `npm run build` → 7 rotas, 3 páginas de projeto pré-renderizadas.


## 2026-08-20 — Campo `tradeoff` no contrato: API, Firestore e frontend

O trade-off ledger passou a ter as duas metades. `Project.Tradeoff` existe no model, é gravado e
lido do Firestore, sai como `tradeoff` no JSON de `GET /api/projects`, e o frontend deixou de
estender o tipo localmente. O seed carrega um custo real em cada projeto de exemplo.

| Arquivo | Ação | Por quê |
|---|---|---|
| `PortfolioApi/Models/Project.cs` | modificado | `Tradeoff` opcional (`:30`), posicionado junto de `Description` porque são as duas metades do mesmo par |
| `PortfolioApi/Services/DataSeeder.cs` | modificado | custo real nos dois projetos semeados — é a única fonte de exemplo do ledger para quem sobe do zero |
| `PortfolioApi.Tests/FirestoreProjectStoreTests.cs` | modificado | round-trip, ausência, documento legado sem a chave, e seed com custo |
| `PortfolioApi.Tests/ProjectContractTests.cs` | criado | trava o nome `tradeoff` no JSON; sem DTO (C-4), renomear a propriedade muda o contrato calado |
| `frontend/src/lib/api.ts` | modificado | `tradeoff?: string` no tipo `Project` |
| `frontend/src/components/Projects.tsx` | modificado | alias `ProjectRecord` removido: o campo agora vem do contrato de verdade |
| `frontend/src/__tests__/design-system.test.ts` | modificado | asserção TR-05: o alias local não pode voltar |
| `.specs/features/project-tradeoff-field/` | criado | spec TR-01..TR-05 com matriz e o risco coberto pelo TR-02 |
| `.specs/codebase/TESTING.md` | modificado | contagem do gate `full` estava em 17 e agora são 40; seção nova sobre os testes que rodam sem emulador |
| `CLAUDE.md` | modificado | `Tradeoff` é opcional por construção — documento antigo sem a chave lê como null |

**Sem migration, e o risco não é esse.** O Firestore é schemaless: o campo novo não exige DDL nem
backfill, e nenhum dado de produção foi tocado. O risco real é o inverso — os documentos que já
estão lá **não têm** a chave `tradeoff`, e se `ConvertTo<Project>()` estourasse com ela ausente,
`GET /api/projects` quebraria inteiro no primeiro deploy. Um teste que grava e lê pelo próprio
model nunca pegaria isso, porque o model sempre grava a chave. Por isso
`FirestoreProjectStoreTests.cs:176` escreve um documento **cru**, via dicionário, sem a chave, e
prova que a leitura devolve null sem lançar.

**Verificado ponta a ponta, não só em unitário.** Com o emulador no ar, a API real subiu contra
ele, o seed rodou, `GET /api/projects` devolveu `"tradeoff"` em camelCase nos dois documentos, e o
frontend renderizou as duas linhas de custo. A coleção usada no e2e foi `projects_e2e_tradeoff`,
no emulador — produção intacta.

**Não toquei:** os documentos de produção continuam sem `tradeoff`, então em
`pedrocpdev.vercel.app` o ledger segue mostrando só a linha "Kept" até você preencher o campo no
console do Firebase. Isso é escrita em dado real e é decisão sua, não minha. Não criei endpoint de
escrita nem painel admin (fora de escopo no nível projeto). Não tornei o campo obrigatório.

**Gate:** `dotnet test PortfolioApi.Tests` → 18 passed (com emulador) · `npx vitest run` → 22 passed ·
`npx eslint .` → exit 0 · `npm run build` → compiled successfully.


## 2026-08-20 — Redesign visual do frontend: "The Decision Record"

O portfólio deixou de parecer template de dev. A página passou a ter a forma do artefato que
melhor representa o Pedro como engenheiro — o registro de decisão com o custo declarado — com
paleta em tokens (oliva-grafite + âmbar + clay), três famílias tipográficas com papéis fixos,
spine assimétrico e o *trade-off ledger* como elemento-assinatura. Nenhuma linha de
`src/lib/api.ts`, do contrato de `/api/projects` ou da API .NET foi tocada.

| Arquivo | Ação | Por quê |
|---|---|---|
| `frontend/src/app/globals.css` | modificado | os 7 tokens de cor, os 3 stacks de fonte, `:focus-visible:60` e o bloco `prefers-reduced-motion:73` — a fundação de que todo componente depende |
| `frontend/src/app/layout.tsx` | modificado | troca Outfit por Bricolage + Newsreader + JetBrains; variáveis do `next/font` no `<html>` (L-007) |
| `frontend/src/app/page.tsx` | modificado | container sai do wrapper e vai para cada seção, para as bordas irem de ponta a ponta; footer reescrito |
| `frontend/src/components/Section.tsx` | criado | primitiva do spine: dono único do grid e do espaçamento, para as seções não definirem padding próprio e brigarem entre si |
| `frontend/src/components/Ledger.tsx` | criado | o elemento-assinatura; extraído porque Hero e Projects usam o mesmo par decisão/custo |
| `frontend/src/components/SectionHeader.tsx` | removido | existia para renderizar o ordinal `01/02/03` de seções que não são sequência |
| `frontend/src/components/Hero.tsx` | modificado | tese no lugar do cargo genérico, e o AD-001 real logo abaixo como prova imediata |
| `frontend/src/components/About.tsx` | modificado | nuvem de 19 tags vira Core (6, com peso) + Also worked with (5, discreto) |
| `frontend/src/components/Projects.tsx` | modificado | cards viram registros com ledger; `tradeoff` estendido localmente para não tocar em `api.ts`; estado vazio vira convite |
| `frontend/src/components/Contact.tsx` | modificado | copy e links no vocabulário novo; Instagram removido do conjunto profissional |
| `frontend/src/components/ContactForm.tsx` | modificado | labels reais em vez de placeholder como rótulo, `aria-live` no status, e o verbo sobrevive até a confirmação |
| `frontend/src/components/Navbar.tsx` | modificado | sentence case e tokens; `Projects` vira `Work` para casar com a seção |
| `frontend/src/__tests__/design-system.test.ts` | criado | 16 invariantes mecânicas do sistema de design |
| `.specs/features/visual-decision-record/` | criado | spec (DR-01..DR-11), design e tarefas da feature |
| `.specs/codebase/CONVENTIONS.md` | modificado | a regra antiga proibia literalmente o que o AD-007 decidiu fazer |
| `.specs/project/STATE.md` | modificado | AD-007 (a decisão e o que ela custou), L-007 e uma ideia adiada |
| `CLAUDE.md` | modificado | descrevia a paleta dark-navy que deixou de existir |

**O gate não pega design, e quase escondeu um bug.** Com as variáveis do `next/font` no
`<body>`, `--font-body` (declarado em `:root`) referenciava um `--font-newsreader` que não existe
naquele escopo. O token virava inválido, o `font-family` do body era descartado, e toda a prosa
caía no sans padrão — com **build, lint e 21 testes verdes**. Só apareceu ao olhar o pixel. Está
em L-007, e `design-system.test.ts:60` guarda a correção.

**Reserva do clay.** `--clay` só pode aparecer na linha de custo do ledger e em mensagem de erro.
Não é convenção escrita torcendo para ser seguida: `design-system.test.ts:156` falha se qualquer
outro componente usar. É o que impede o acento de virar decoração e a assinatura de se diluir.

**Evidência, sem inflar:** 21/21 testes, lint 0, build ok. Mas DR-04 (spine) e DR-11 (hierarquia
de tecnologias) são composição visual e o gate **não** os cobre — não há jsdom no projeto.
Estão marcados na matriz como *verificado por inspeção*, com screenshot em 1440px e 390px.

**Não toquei:** `src/lib/api.ts`, o contrato de `/api/projects` e a API .NET inteira — o campo
`tradeoff` é opcional e estendido no componente, então nenhum projeto existente quebra e o
ledger simplesmente não renderiza o bloco de custo quando o campo não vem. Não adicionei
`@testing-library`/`jsdom` (fora de escopo). Não corrigi `CLAUDE.md:23` ("No test runner is
configured"), que é falso desde que o vitest entrou — está registrado em Ideias Adiadas.
`frontend/AGENTS.md` e `frontend/CLAUDE.md`, gerados pelo `next dev`, ficaram fora do commit.

**Gate:** `npx vitest run` → 21 passed (2 files) · `npx eslint .` → exit 0 ·
`npm run build` → compiled successfully, 4/4 páginas estáticas.


## 2026-08-18 — Backlog da auditoria de segurança registrado em CONCERNS.md

Os achados da auditoria de segurança deixaram de existir só na conversa: viraram C-5..C-12 em
`codebase/CONCERNS.md`, na ordem de correção acordada, com `arquivo:linha` em cada um. Nenhum
código foi tocado — é registro, não correção.

| Arquivo | Ação | Por quê |
|---|---|---|
| `.specs/codebase/CONCERNS.md` | modificado | C-5..C-12 acrescentados sob um cabeçalho de auditoria, mais a linha de base do que já está correto e não pode regredir |
| `.specs/project/ROADMAP.md` | modificado | ponteiro para o backlog; sem ele o registro não sobreviveria a uma troca de sessão |

**Por que o ponteiro no ROADMAP era necessário:** o carregamento base do harness é
`PROJECT.md` + `ROADMAP.md` + `STATE.md`. `codebase/CONCERNS.md` não entra nele — registrar o
backlog só lá deixaria o arquivo correto e invisível. O ROADMAP é o que faz a próxima sessão
saber que ele existe.

**O que foi registrado além dos problemas:** a seção de auditoria abre com o que a varredura
confirmou estar **certo** (segredos ausentes do histórico, docs de API fechadas em produção,
ausência de endpoints de escrita, React escapando por padrão, `dotnet list --vulnerable` limpo).
Sem isso, uma sessão futura não teria como distinguir "não auditado" de "auditado e aprovado",
e poderia regredir um acerto sem perceber.

**Não toquei:** nenhum arquivo de código, nenhum item do backlog foi corrigido nesta tarefa —
C-5 continua aberto e é o próximo. C-1..C-4 ficaram como estavam; são anteriores e não vieram
desta auditoria, então não entraram na renumeração.

**Gate:** não se aplica — a mudança é exclusivamente documental, sem código, build ou teste
envolvido. `git diff --stat` confirma: só dois arquivos `.specs/`.

**Commit:** `docs(specs): registrar backlog da auditoria de seguranca em concerns`

---

## 2026-08-18 — Next 16.3.1 elimina os 4 CVEs high do bundle de produção

O frontend deixou de subir para produção com dependências vulneráveis: `npm audit --omit=dev`
saiu de 4 vulnerabilidades high para zero. Nenhum comportamento da aplicação mudou — é troca
de versão de dependência, sem alteração de código-fonte.

| Arquivo | Ação | Por quê |
|---|---|---|
| `frontend/package.json:14,25` | modificado | `next` e `eslint-config-next` fixados em 16.3.1; o 16.2.9 arrastava `postcss@8.4.31` e `sharp@0.34.5`, ambos com advisory high |
| `frontend/package-lock.json` | modificado | regenerado por `npm install`: `sharp` 0.34.5 → 0.35.3, `postcss` aninhado do Next 8.4.31 → corrigido, mais 16 pacotes transitivos |
| `.specs/project/STATE.md` | modificado | Ideia Adiada registrando por que `npm audit` (sem `--omit=dev`) ainda mostra 4 achados |

**CVEs eliminados:** `postcss <=8.5.22` (XSS via `</style>` no stringify e três variantes de
path traversal por `sourceMappingURL`) e `sharp <0.35.0` (libvips: CVE-2026-33327, CVE-2026-33328,
CVE-2026-35590, CVE-2026-35591).

**Por que pin explícito e não `npm audit fix --force`:** o `--force` chega ao mesmo 16.3.1, mas
sem deixar no `package.json` o registro de qual versão foi escolhida e por quê. `eslint-config-next`
subiu junto por exigência da própria Next de acompanhar a versão do framework — é devDependency,
não entrava no escopo do CVE.

**Não toquei:** `postcss`, `brace-expansion` e `js-yaml` que seguem vulneráveis via
`@tailwindcss/postcss`, `vitest` e a cadeia do eslint. São devDependencies — não entram no bundle
servido ao visitante, e é por isso que `npm audit --omit=dev` dá zero enquanto `npm audit` dá 4.
Tratá-los agora seria escopo além do item auditado. Registrado como Ideia Adiada. Nenhum arquivo
de código-fonte foi alterado: o diff é só as duas linhas do `package.json` e o lock regenerado.

**Gate (frontend, `TESTING.md`):**
```
npm audit --omit=dev  → found 0 vulnerabilities   (antes: 4 high)
npm test              → 1 arquivo, 5 testes, 5 passed
npm run build         → Next.js 16.3.1, Compiled successfully in 3.2s, 4/4 páginas, exit 0
npm run lint          → exit 0, sem saída
```

**Commit:** `fix(frontend): atualiza next para 16.3.1 e elimina cves de postcss e sharp`

---

## 2026-08-18 — Alinhamento da documentação ao estado real do repositório

A documentação do repositório passou a descrever o que o código faz hoje: Firestore em vez de
Postgres, e-mail via Resend em vez de stub, e o keep-alive registrado como mecanismo permanente
em vez de só como entrada de journal.

| Arquivo | Ação | Por quê |
|---|---|---|
| `README.md` | modificado | descrevia PostgreSQL/EF Core/migrations — nada disso existe desde a migração |
| `CLAUDE.md:56,58,65,77` | modificado | `EmailService` não é stub desde b1d321b; faltavam `/health`, retry e keep-alive |
| `.specs/codebase/INTEGRATIONS.md` | modificado | keep-alive ganhou seção "Como funciona" permanente (gatilho, ordem, sinal de vida, onde desligar) |
| `.specs/codebase/ARCHITECTURE.md` | modificado | fluxo de dados sem `/health`; retry do frontend não aparecia |
| `.specs/codebase/CONCERNS.md` | modificado | C-1 marcado ALTO já mitigado; C-2 dizia "ausência total de testes" |
| `.specs/codebase/STACK.md` | modificado | frontend constava "sem test runner"; existe vitest |
| `.specs/codebase/STRUCTURE.md` | modificado | faltavam `PortfolioApi.Tests/`, `__tests__/`, workflows, `.dockerignore` |
| `.specs/codebase/CONVENTIONS.md` | modificado | a regra do fetch engolido agora menciona o retry antes da desistência |
| `.specs/project/STATE.md` | modificado | B-001 e a seção Pendências removidos a pedido do usuário; L-004..L-006 reordenadas |
| `.specs/project/ROADMAP.md` | modificado | Firestore constava EM ANDAMENTO e cold start como não especificado |
| `.specs/project/PROJECT.md` | modificado | restrição do Render apontava para B-001, que deixou de existir |
| `.specs/features/firestore-migration/tasks.md` | modificado | 22 critérios sem marcar apesar das evidências APROVADO |
| `.specs/features/*/spec.md` | modificado | remover referências órfãs a B-001 |

**Decisões do usuário registradas:** o Supabase só tinha os 2 projetos de exemplo, então a ideia
adiada de migrar dados reais foi encerrada; o segundo problema mencionado na migração já estava
resolvido; a revogação da senha de app do Gmail ficou com o usuário; B-001 foi excluído por
escolha dele — o resíduo do cold start segue documentado em C-1, AD-005 e PROJECT.md.

**Sobre marcar os checkboxes de `tasks.md`:** não houve nova execução de gate. As marcações
citam as evidências já existentes em `evidencias/`, e o cabeçalho do arquivo diz isso
explicitamente para não simular um gate que não rodou nesta tarefa.

**Não toquei:** entradas anteriores do JOURNAL, que citam B-001 — é append-only e reescrever
histórico seria pior que a referência morta. Nenhum arquivo de código foi alterado: o diff é
100% `.md`.

✓ Gate: nenhum, tarefa exclusivamente documental — `git diff --name-only` confirma só `.md`
✓ Commit: `docs: alinhar documentacao ao estado real do repositorio`

---

## 2026-08-18 — Migração concluída e confirmada em produção

Portfólio servindo projetos do Firestore, formulário de contato entregando e-mail, e a API
não depende mais de um banco que pausa.

| Arquivo | Ação | Por quê |
|---|---|---|
| `PortfolioApi/.dockerignore` | criado | impedir que credenciais entrem na imagem em build local |
| `PortfolioApi/appsettings.json` | modificado | alinhar ao que o código lê (não versionado) |
| `.specs/project/STATE.md` | modificado | fechar B-002 e B-003, registrar L-006 |

**Confirmação do usuário:** e-mail chegou, projetos aparecendo. Encerra B-001 na prática
para a janela coberta pelo keep-alive.

**Armadilha registrada como L-006:** o `Microsoft.NET.Sdk.Web` copia qualquer `*.json` do
projeto para a saída do publish. Um `docker build` local assou a service account e o
`appsettings.json` dentro da imagem — produção nunca afetada, pois o Render constrói do
clone do git. O `.gitignore` não cobre isso porque o Docker lê do disco.

**Não toquei:** `Email:ApiKey` local segue vazia — a chave da Resend é do usuário e só
afeta desenvolvimento; produção já está correta.

✓ Produção: `/health` 200 · `/api/projects` 200 com 2 docs · site com cards · keep-alive 200
✓ Commit: `chore(api): impedir que credenciais entrem na imagem docker`

---

## 2026-08-18 — Cold start do Render deixa de esvaziar a seção de projetos

Um cold start da API não faz mais o portfólio aparecer sem projetos; e o keep-alive
mantém a instância acordada na janela de maior tráfego sem estourar a cota do Render.

| Arquivo | Ação | Por quê |
|---|---|---|
| `PortfolioApi/Endpoints/HealthEndpoints.cs` | criado | alvo de ping que responde sem consultar o Firestore |
| `PortfolioApi/Program.cs` | modificado | registrar o endpoint de health |
| `frontend/src/lib/api.ts:17-42` | modificado | insistir durante o religamento em vez de desistir em 8s |
| `frontend/src/lib/__tests__/api.test.ts` | criado | RED: 3 casos de retry falhando antes da implementação |
| `frontend/package.json` | modificado | vitest, para haver como provar lógica de frontend |
| `.github/workflows/keep-api-awake.yml` | criado | manter a instância acordada dentro da cota gratuita |
| `.specs/features/render-cold-start/` | criado | spec, matriz e evidências da feature |

**Por que a janela de 16h e não 24/7:** o Render concede 750 instance-hours/mês e um
serviço dormindo não consome nenhuma. Pingar o tempo todo consumiria ~744 h num mês de
31 dias — margem de 6 h. Estourar a cota faz o Render suspender **todos** os serviços free
até o mês seguinte, o que trocaria uma falha intermitente por uma falha total. Ver AD-005.

**Por que um `/health` em vez de pingar `/api/projects`:** o ping roda ~96 vezes por dia;
usar o endpoint de dados gastaria cota de leitura do Firestore sem ganho e faria o
keep-alive falhar junto com o banco. Ver AD-006.

**Efeito colateral conhecido:** com o retry, uma chamada a `getProjects()` com a API fora
do ar agora leva ~54s antes de desistir, contra 8s antes. Isso atrasa o `next build`
quando a API está indisponível no momento do build.

**Não toquei:** a seção `Email` do `appsettings.json` — está quebrada desde b1d321b (B-003),
mas contém credencial real e não é parte desta tarefa. Também não movi a leitura de
projetos para o Next.js: resolveria o resíduo da madrugada, mas contraria AD-002.

✓ Gate Full: `dotnet build` 0 warnings · API 12/12 · vitest 5/5 · lint e build limpos
✓ Credencial real validada contra o Firestore de produção (somente leitura)
✓ Commit: `feat: absorver cold start do render sem esvaziar a vitrine`

---

## 2026-08-18 — Proteger service account do Firebase contra commit acidental

Chave privada real adicionada ao repositório pelo usuário passou a ser ignorada pelo git,
por padrão amplo em vez de caminho único.

| Arquivo | Ação | Por quê |
|---|---|---|
| `.gitignore:8-14` | modificado | qualquer service account do Firebase/GCP passa a ser ignorada, não só este arquivo |
| `.specs/JOURNAL.md` | criado | primeiro registro do log append-only de mudanças |
| `.specs/project/STATE.md` | modificado | registrar auditoria do histórico (B-002) e config local defasada (B-003) |

**Por que padrão amplo e não o nome do arquivo:** ignorar
`PortfolioApi/portfoliodb-9215c-firebase-adminsdk-fbsvc-d864c5e256.json` protegeria só esta
chave. Gerar uma nova (nome diferente) ou salvá-la em outra pasta voltaria a expor. Os padrões
`*firebase-adminsdk*.json`, `*serviceAccount*.json`, `*service-account*.json` e
`gcp-credentials*.json` cobrem o formato, não a instância.

**Auditoria feita antes de qualquer coisa:** a chave estava como untracked e o histórico
completo (`git rev-list --all`) não contém `BEGIN PRIVATE KEY` nem `private_key` em nenhum
commit. **Não houve vazamento; rotação não é necessária.**

**Não toquei:** `PortfolioApi/appsettings.json` — contém secrets reais do usuário e está
defasado (ver B-003); alterar config com credencial real exige confirmação dele.
Também não movi a chave para fora do repositório: é decisão do usuário.

✓ Verificação: `git check-ignore -v` confirma a regra ativa; a chave sumiu do `git status`.
✗ Commit: não commitado — usuário ainda não autorizou e o branch é `main`.

---

## 2026-08-18 — Migrar persistência de PostgreSQL/Supabase para Cloud Firestore

`GET /api/projects` e `/api/projects/{id}` passam a ler do Firestore, e o banco indisponível
deixa de derrubar a API inteira no startup.

| Arquivo | Ação | Por quê |
|---|---|---|
| `PortfolioApi/PortfolioApi.csproj` | modificado | trocar EF Core + Npgsql por `Google.Cloud.Firestore` 4.4.0 |
| `PortfolioApi/Program.cs` | modificado | compor `FirestoreDb` + store; seed que loga e segue em vez de derrubar |
| `PortfolioApi/Models/Project.cs` | modificado | mapear para documento; `Id` string; normalizar `DateTime` para UTC |
| `PortfolioApi/Data/FirestoreProjectStore.cs` | criado | concentrar todo acesso ao Firestore num ponto testável |
| `PortfolioApi/Data/FirestoreOptions.cs` | criado | credencial por env var, sem arquivo em disco no Render |
| `PortfolioApi/Data/AppDbContext.cs` | removido | sem EF Core não há contexto |
| `PortfolioApi/Migrations/` (3 arquivos) | removido | Firestore é schemaless |
| `PortfolioApi/Endpoints/ProjectsEndpoints.cs` | modificado | injetar a store; rota `{id}` sem `:int` |
| `PortfolioApi/Services/DataSeeder.cs` | modificado | idempotência por contagem da coleção |
| `PortfolioApi/appsettings.Example.json` | modificado | exemplo da seção `Firestore` no lugar de `ConnectionStrings` |
| `PortfolioApi.Tests/` (3 arquivos) | criado | primeiro projeto de testes do repo; 12 casos contra emulador |
| `frontend/src/lib/api.ts:4` | modificado | id do Firestore é string, não number |
| `CLAUDE.md` | modificado | refletir a nova stack e o comando de testes |
| `.specs/` | criado | mapeamento do codebase + spec/design/tasks/evidências |

**Por que uma store nova:** o projeto não tinha camada de repositório (endpoints chamavam
`AppDbContext` direto). `FirestoreDb` não é substituível em teste como um `DbContext`
InMemory seria, então a store preserva o espírito da convenção — uma peça de dados injetada —
e viabiliza os testes. Ver AD-002.

**Efeito colateral conhecido:** `id` deixou de ser `int` e virou `string` — quebra de contrato
público da API, propagada ao frontend.

**Não toquei:** `EmailService`, `ContactEndpoints` e componentes visuais — fora do escopo.
Cold start do Render segue aberto como B-001; a migração não o resolve.

✓ Gate Full: `dotnet build` 0 warnings · `dotnet test` 12/12 · `npm run lint` + `build` limpos
✓ E2E HTTP real: FS-01 a FS-07 verificados (`.specs/features/firestore-migration/evidencias/`)
✗ Commit: não commitado — usuário ainda não autorizou.
