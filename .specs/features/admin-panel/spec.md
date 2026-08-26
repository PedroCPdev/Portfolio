# Spec: Painel administrativo — autenticação e CRUD de projetos

Escopo classificado como: **Grande** (dois sub-projetos, autenticação nova, endpoints de escrita
numa API pública, ~14 arquivos)

**O que muda no projeto:** `project/PROJECT.md` lista hoje "Área administrativa / CRUD autenticado
de projetos (escrita é manual no console do Firestore)" como **fora de escopo de nível projeto**.
Este pedido revoga essa linha — a revogação é registrada como decisão em `STATE.md`, não apagada
em silêncio. A ideia adiada correspondente ("Endpoints de escrita / painel admin", 2026-08-18)
sai do backlog e vira esta feature.

**O problema em uma frase:** cadastrar um projeto hoje é digitar sete campos à mão no console do
Firebase — três deles (`title`/`why`/`cost`) por decisão, num array de mapas — o que fez o próprio
AD-008 registrar "dobra o custo de digitação no console do Firebase, sem painel admin" como custo
assumido.

## Decisões de escopo já tomadas com o usuário

Levadas a ele antes de escrever esta spec (ver `context.md`):

1. Autenticação por **senha + JWT emitido pela própria API** — não Firebase Auth, não chave estática.
2. Painel na **rota `/admin` do próprio site**, mesmo deploy da Vercel.
3. **CRUD completo**: criar, editar, excluir projeto e editor de decisões com reordenação.

## Requisitos

### P1 — MVP (fatia vertical: entrar, criar um projeto com decisões, vê-lo no site)

**Autenticação — API**

**[ADM-01]**
QUANDO chega um `POST /api/admin/login` com a senha correta
ENTÃO o sistema DEVE responder `200` com `{ token, expiresAt }`, onde `token` é um JWT assinado
que os endpoints de escrita aceitam

**[ADM-02]**
QUANDO chega um `POST /api/admin/login` com senha incorreta, ausente ou vazia
ENTÃO o sistema DEVE responder `401` com `{ success: false, error }` genérico, sem distinguir
"senha errada" de "admin não configurado" e sem emitir token

**[ADM-03]**
QUANDO chegam mais de 5 requisições a `POST /api/admin/login` dentro de uma janela de 1 minuto
ENTÃO o sistema DEVE recusar as excedentes com `429` sem chegar a verificar a senha

**[ADM-04]**
QUANDO a configuração de admin está ausente ou vazia (`Admin:PasswordHash` ou `Admin:JwtSigningKey`)
ENTÃO o sistema DEVE responder `401` a qualquer login e a qualquer escrita — **nunca** liberar por
falta de configuração (o oposto do degradar-em-silêncio de `EmailService`, C-9)

**[ADM-05]**
QUANDO chega uma requisição a endpoint de escrita sem `Authorization`, ou com token malformado,
com assinatura inválida, ou expirado
ENTÃO o sistema DEVE responder `401` e NÃO DEVE gravar nada no Firestore

**Escrita — API**

**[ADM-06]**
QUANDO chega um `POST /api/projects` autenticado com corpo válido
ENTÃO o sistema DEVE gravar o documento, responder `201` com `Location: /api/projects/{id}` e o
projeto criado no corpo, com `createdAt` em UTC

**[ADM-07]**
QUANDO chega um `PUT /api/projects/{id}` autenticado com corpo válido para um id existente
ENTÃO o sistema DEVE sobrescrever os campos editáveis e responder `200` com o projeto atualizado,
preservando `id` e `createdAt` originais

**[ADM-08]**
QUANDO chega um `PUT` ou `DELETE /api/projects/{id}` autenticado para um id que não existe
ENTÃO o sistema DEVE responder `404` e NÃO DEVE criar documento

**[ADM-09]**
QUANDO chega um `DELETE /api/projects/{id}` autenticado para um id existente
ENTÃO o sistema DEVE apagar o documento, responder `204`, e o `GET /api/projects/{id}` seguinte
DEVE responder `404`

**[ADM-10]**
QUANDO chega um `POST` ou `PUT` autenticado com corpo inválido — `title` ou `description` vazios;
qualquer campo acima do limite da tabela abaixo; url que não seja `http`/`https`; decisão sem
`title` ou sem `why`
ENTÃO o sistema DEVE responder `400` com `{ success: false, error }` e NÃO DEVE gravar nada

**[ADM-11]**
QUANDO um projeto é gravado com uma lista de decisões
ENTÃO o sistema DEVE preservar exatamente a ordem enviada na leitura seguinte
(a ordem é informação, não detalhe de armazenamento — AD-008)

**[ADM-12]**
QUANDO chega um `GET /api/projects` ou `GET /api/projects/{id}` sem `Authorization`
ENTÃO o sistema DEVE responder `200` com o mesmo contrato de hoje — a leitura pública não regride

**Painel — frontend**

**[ADM-13]**
QUANDO abro `/admin` sem sessão válida
ENTÃO o painel DEVE mostrar apenas o formulário de senha — nenhuma lista de projetos, nenhuma ação

**[ADM-14]**
QUANDO envio a senha errada no formulário de login
ENTÃO o painel DEVE mostrar a mensagem de erro e NÃO DEVE guardar token nenhum

**[ADM-15]**
QUANDO estou autenticado em `/admin`
ENTÃO o painel DEVE listar os projetos existentes, cada um com ação de editar e de excluir, e
oferecer a ação de criar projeto

**[ADM-16]**
QUANDO preencho o formulário de projeto (título, descrição, tags, GitHub, live, imagem) com as
decisões e salvo com sucesso
ENTÃO o painel DEVE confirmar e voltar à lista já com o projeto novo

**[ADM-17]**
QUANDO uso o editor de decisões
ENTÃO ele DEVE permitir adicionar, editar, remover e mover uma decisão para cima ou para baixo,
e a ordem exibida DEVE ser a ordem enviada à API

**[ADM-18]**
QUANDO a API responde `401` durante qualquer ação do painel
ENTÃO o painel DEVE descartar a sessão e voltar ao login com aviso de sessão expirada

**[ADM-19]**
QUANDO `/admin` é servido
ENTÃO a rota DEVE declarar `noindex, nofollow` e NÃO DEVE ser linkada de nenhuma página pública

### P2 — edição e remoção (mesma feature, ordem de implementação depois do P1)

**[ADM-20]**
QUANDO abro um projeto existente para editar
ENTÃO o formulário DEVE vir preenchido com os valores atuais, decisões inclusive

**[ADM-21]**
QUANDO peço para excluir um projeto
ENTÃO o painel DEVE exigir que eu digite o título exato e manter a confirmação desabilitada
enquanto o texto não bater

**[ADM-22]**
QUANDO uma ação do painel falha por erro da API ou de rede (4xx, 5xx, timeout)
ENTÃO o painel DEVE mostrar o erro na tela — nunca engolir a falha como faz `getProjects` no site
público, onde o silêncio é proposital e aqui seria perda de dado

## Limites de entrada escolhidos

| Campo | Limite | Razão |
|---|---|---|
| `title` | 120 | é manchete de card e `<h1>` da página de detalhe |
| `description` | 2000 | parágrafo de vitrine, não artigo |
| `tags` | 12 itens × 30 chars | o card já fica poluído bem antes disso |
| `githubUrl` / `liveUrl` / `imageUrl` | 2048, esquema `http`/`https` | limite prático de URL; esquema fechado barra `javascript:` num `href` |
| `decisions` | 10 itens | acima disso a página de detalhe deixa de ser leitura |
| `decision.title` | 120 | mesma função de manchete |
| `decision.why` | 1500 | prosa da decisão |
| `decision.cost` | 1500 | opcional (AD-008), mesmo teto do `why` |
| corpo da requisição | 64 KB | pior caso legítimo ~35 KB (descrição + 10 decisões em UTF-8); o dobro de folga |

## Fora de escopo

- **Revalidação sob demanda do ISR.** Salvar no painel não força a home a atualizar: ela usa
  `next: { revalidate: 60 }`, então a mudança aparece em até 60s. Um webhook de `revalidatePath`
  é feature própria — vai para Ideias Adiadas.
- **Upload de imagem.** `imageUrl` continua sendo URL digitada; não haverá storage de arquivo.
- **Múltiplos usuários, cadastro, recuperação de senha, 2FA.** Um usuário só; trocar a senha é
  trocar a variável de ambiente.
- **Rascunho/publicação.** Projeto salvo é projeto no ar; não há estado de rascunho.
- **Reordenar projetos na vitrine.** A ordem segue `createdAt` decrescente, como hoje.
- **Editor rico / markdown.** Os campos continuam texto puro, como o que o site já renderiza.
- **Log de auditoria** de quem mudou o quê, além do log padrão da aplicação.
- **C-5 (validação do `/api/contact`).** Esta feature constrói o `ValidationFilter<T>` que o C-5
  havia projetado e o aplica **só** aos endpoints novos; `/api/contact` não é tocado e o C-5 segue
  pendente no backlog, agora com metade do caminho pronto.
- **C-6 (rate limit global por endpoint).** A política nova de login herda a mesma limitação,
  conscientemente — ver `design.md`.
- **DTOs (C-4).** A escrita continua desserializando o model direto, conforme `CONVENTIONS.md`.

## Matriz de rastreabilidade

Todos os `arquivo:linha` foram lidos do código no fechamento, não de memória. Gate completo em
`evidencias/20260824-gate-full-APROVADO.md` (159 testes, zero pulados).

| ID | Tarefa | Implementação | Teste | Evidência | Status |
|---|---|---|---|---|---|
| ADM-01 | T2 | `PortfolioApi/Endpoints/AdminEndpoints.cs:14`, `Auth/AdminTokenService.cs:22` | `AdminAuthTests.cs:18` | ✓ | Verificado |
| ADM-02 | T1,T2 | `Endpoints/AdminEndpoints.cs:36`, `Auth/AdminPasswordHasher.cs:33` | `AdminAuthTests.cs:43`, `:59`, `AdminPasswordHasherTests.cs:24` | ✓ | Verificado |
| ADM-03 | T2 | `Auth/LoginRateLimitPolicy.cs:31`, `Endpoints/AdminEndpoints.cs:46` | `AdminAuthTests.cs:98` | ✓ | Verificado |
| ADM-04 | T1,T2 | `Auth/AdminOptions.cs:22`, `Auth/AdminTokenService.cs:40`, `Program.cs:73` | `AdminAuthTests.cs:74`, `:86`, `AdminPasswordHasherTests.cs:38` | ✓ | Verificado |
| ADM-05 | T5 | `Endpoints/ProjectsEndpoints.cs:45`, `:58`, `:64` | `AdminProjectsEndpointTests.cs:163` | ✓ | Verificado |
| ADM-06 | T5 | `Endpoints/ProjectsEndpoints.cs:38` | `AdminProjectsEndpointTests.cs:59`, `:81` | ✓ | Verificado |
| ADM-07 | T4,T5 | `Data/FirestoreProjectStore.cs:58`, `Endpoints/ProjectsEndpoints.cs:49` | `FirestoreProjectStoreTests.cs:266`, `AdminProjectsEndpointTests.cs:101` | ✓ | Verificado |
| ADM-08 | T4,T5 | `Data/FirestoreProjectStore.cs:58`, `:78`, `:97` | `FirestoreProjectStoreTests.cs:285`, `:322`, `AdminProjectsEndpointTests.cs:129` | ✓ | Verificado |
| ADM-09 | T4,T5 | `Data/FirestoreProjectStore.cs:78`, `Endpoints/ProjectsEndpoints.cs:62` | `FirestoreProjectStoreTests.cs:298`, `AdminProjectsEndpointTests.cs:149` | ✓ | Verificado |
| ADM-10 | T3,T5 | `Validation/ValidationFilter.cs:14`, `Validation/ValidateItemsAttribute.cs:20`, `Validation/NotBlankAttribute.cs:12`, `Models/Project.cs`, `Models/Decision.cs` | `ProjectValidationTests.cs:26`–`:141`, `AdminProjectsEndpointTests.cs:190` | ✓ | Verificado |
| ADM-10 (413) | T5 | `Endpoints/ProjectsEndpoints.cs:16`, `:47`, `:60` | `AdminProjectsEndpointTests.cs:218` (metadata) | ✓ + `evidencias/20260824-limite-de-corpo-kestrel.md` | Verificado — o `413` em si é do Kestrel; o TestServer não aplica limite de corpo, então o teste automatizado afirma a declaração e a evidência mostra o servidor honrando |
| ADM-11 | T4,T5 | `Data/FirestoreProjectStore.cs:58` (ordem preservada pelo array do Firestore) | `FirestoreProjectStoreTests.cs:331`, `AdminProjectsEndpointTests.cs:59` | ✓ | Verificado |
| ADM-12 | T5 | `Endpoints/ProjectsEndpoints.cs:21`, `:26` (inalterados) | `AdminAuthTests.cs:113`, `AdminProjectsEndpointTests.cs:237` | ✓ | Verificado |
| ADM-13 | T11 | `components/admin/AdminPanel.tsx:26`, `:96` | `AdminPanel.test.tsx:61`, `:85`, `:189` | ✓ | Verificado |
| ADM-14 | T7,T11 | `lib/admin.ts:91`, `components/admin/LoginForm.tsx:41` | `admin.test.ts:62`, `AdminPanel.test.tsx:72` | ✓ | Verificado |
| ADM-15 | T11 | `components/admin/AdminPanel.tsx:110`, `:167` | `AdminPanel.test.tsx:102`, `:115` | ✓ | Verificado |
| ADM-16 | T7,T9 | `components/admin/ProjectForm.tsx:32`, `lib/admin.ts:111` | `ProjectForm.test.tsx:40`, `:85`, `:113`, `admin.test.ts:90` | ✓ | Verificado |
| ADM-17 | T8 | `components/admin/DecisionEditor.tsx:23`–`:33` | `DecisionEditor.test.tsx:22`–`:96`, `admin.test.ts:107` | ✓ | Verificado |
| ADM-18 | T7,T11 | `lib/admin.ts:140`, `components/admin/AdminPanel.tsx:48` | `admin.test.ts:148`, `AdminPanel.test.tsx:135` | ✓ | Verificado |
| ADM-19 | T11,T12 | `app/admin/layout.tsx:8` | `design-system.test.ts:311`, `:320` | ✓ | Verificado |
| ADM-20 | T9 | `components/admin/ProjectForm.tsx:21`–`:27` | `ProjectForm.test.tsx:66`, `AdminPanel.test.tsx:159` | ✓ | Verificado |
| ADM-21 | T10,T11 | `components/admin/DeleteConfirm.tsx:19`, `:44` | `DeleteConfirm.test.tsx:20`, `:32`, `AdminPanel.test.tsx:171` | ✓ | Verificado |
| ADM-22 | T7,T9,T11 | `lib/admin.ts:129`, `components/admin/ProjectForm.tsx:146`, `AdminPanel.tsx:144` | `admin.test.ts:160`, `:175`, `ProjectForm.test.tsx:98`, `AdminPanel.test.tsx:125` | ✓ | Verificado |

Status possíveis: Não iniciado · Em andamento · Verificado · Não atendido

**22 de 22 verificados.** Nenhum requisito ficou com implementação sem teste ou com teste sem
implementação.
