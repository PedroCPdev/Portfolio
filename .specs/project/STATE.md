# STATE.md

Memória persistente do projeto.

## Decisões (AD-NNN)

### AD-001: Migrar persistência de PostgreSQL/Supabase para Cloud Firestore
- **Decisão:** trocar EF Core + Npgsql por `Google.Cloud.Firestore` 4.4.0.
- **Razão:** o free tier do Supabase pausa por inatividade; com o banco pausado o
  `MigrateAsync()` no startup lançava e a API inteira não subia no Render. O free tier
  do Firestore (Spark) não pausa. Dados são poucos e simples — NoSQL serve bem.
- **Trade-off:** perde-se SQL/joins e migrations versionadas; ganha-se disponibilidade.
  Contrato público muda: `id` deixa de ser `int` e passa a ser `string`.
- **Impacto:** afeta API e frontend. Decidido em 2026-08-18 pelo usuário.

### AD-002: Manter a leitura do Firestore dentro da API .NET
- **Decisão:** o Next.js continua falando com a API; a API fala com o Firestore.
  Alternativa avaliada e recusada: Next ler o Firestore direto.
- **Razão:** escolha explícita do usuário — preserva a arquitetura atual e mantém a
  API em C# como peça demonstrável do portfólio.
- **Trade-off:** não resolve por si só o cold start do Render — tratado depois pela
  feature `render-cold-start` (keep-alive + retry), não pela troca de banco.
- **Impacto:** `GET /api/projects` permanece no caminho crítico da página.

### AD-003: Falha de seed não derruba mais o startup
- **Decisão:** a FASE 2 do `Program.cs` loga e segue em vez de `throw`.
- **Razão:** era a causa raiz do incidente — indisponibilidade do banco derrubava
  inclusive `/api/contact`, que não usa banco.
- **Trade-off:** a API pode subir "saudável" servindo lista vazia de projetos.
- **Impacto:** degradação parcial em vez de queda total.

### AD-004: Credencial do Firestore via JSON em variável de ambiente
- **Decisão:** `Firestore__CredentialsJson` com o service account inteiro; fallback
  para Application Default Credentials.
- **Razão:** Render não oferece disco conveniente para arquivo de credencial.
- **Trade-off:** JSON grande numa env var; exige cuidado para nunca commitar.
- **Impacto:** `appsettings.json` segue gitignored.

### AD-005: Keep-alive em janela de 16h, não 24/7
- **Decisão:** pingar `/health` a cada 10 min apenas entre 08:00 e 00:00 (BRT).
- **Razão:** o Render dá 750 instance-hours/mês e serviço dormindo não consome. 24/7 daria
  744 h/mês num mês de 31 dias — margem de 6 h. Estourar a cota faz o Render **suspender
  todos os serviços free até o mês seguinte**, trocando falha intermitente por falha total.
- **Trade-off:** madrugada continua com cold start; absorvido pelo retry do frontend.
- **Impacto:** consumo ~496 h/mês, margem de 254 h. Janela editável numa linha do workflow.

### AD-006: `/health` dedicado em vez de pingar `/api/projects`
- **Decisão:** endpoint próprio que não toca no Firestore.
- **Razão:** o ping roda ~96×/dia; usar `/api/projects` gastaria cota de leitura do banco
  sem ganho, e faria o keep-alive falhar junto com o banco.
- **Trade-off:** um endpoint a mais na superfície pública (sem dados, fora do OpenAPI).
- **Impacto:** ping responde em ~9 ms mesmo com o Firestore fora do ar.

### AD-007: Redesign visual "The Decision Record" — paleta em tokens
- **Decisão:** substituir a paleta dark-navy hex-literal (`#050d1a`/`#0d1b2e`/`#5ba0f5`) por um
  sistema de tokens em `globals.css` (oliva-grafite `--ground`, âmbar `--amber`, `--clay`), e
  trocar Outfit por Bricolage Grotesque (display) + Newsreader (corpo) + JetBrains Mono (utilitário).
- **Razão:** o design anterior era a soma dos tells de portfólio-template — `// hello world`,
  `01/02/03` em seções que não são sequência, o mesmo card repetido 5×, nuvem de 19 tags com peso
  idêntico. Não comunicava o que diferencia o Pedro: decidir e registrar o que a decisão custou.
  A página passa a ter a forma dos próprios `AD-NNN`.
- **Trade-off:** revoga a regra de `CONVENTIONS.md` que proibia cores novas e tokens temáticos —
  regra que existia para dar consistência e que a paleta nova cumpre melhor via tokens. Serifa
  (Newsreader) no escuro é risco de legibilidade assumido conscientemente. Toda a superfície visível
  mudou de uma vez, então não há como comparar A/B com o design anterior em produção.
- **Impacto:** 8 componentes reescritos, `SectionHeader` virou `Section` + `Ledger`. Nenhuma mudança
  em `src/lib/api.ts`, no contrato de `/api/projects` ou na API .NET. Decidido em 2026-08-20 pelo usuário.

### AD-008: Separar vitrine de raciocínio — página por projeto
- **Decisão:** a home volta a ser vitrine (card por projeto, grid responsivo) e o raciocínio de
  engenharia migra para `/projects/{id}`. O campo `tradeoff` (string única, AD-007) é substituído
  por `decisions: Decision[]`, cada uma com `title`, `why` e `cost`.
- **Razão:** com o ledger na home, a primeira tela era longa e o visitante tinha que ler três
  trade-offs antes de saber quais projetos existem. Separar dá à vitrine a função de vitrine e
  ao raciocínio o espaço que ele precisa — inclusive para mais de uma decisão por projeto,
  que a string única não comportava. Pedido do usuário em 2026-08-20.
- **Trade-off:** o argumento mais forte do portfólio deixa de estar na primeira tela e passa a
  exigir um clique. A aposta é que o rótulo "3 decisions →" no card compra esse clique. Também
  dobra o custo de digitação no console do Firebase: são três campos por decisão, sem painel admin.
- **Impacto:** contrato público muda de novo (`tradeoff` sai, `decisions` entra) — ninguém em
  produção usava `tradeoff`, então não há dado a migrar. Rota nova pré-renderizada por
  `generateStaticParams`. `Ledger.tsx` vira `DecisionRecord.tsx`.

### AD-009: Largura da página em `.shell` / `.measure`, não em componente
- **Decisão:** duas classes em `globals.css` passam a ser as únicas donas de largura:
  `.shell` (contêiner, 96rem) e `.measure` (prosa, 65ch). `max-w-5xl` some do código.
- **Razão:** o layout anterior prendia tudo em 64rem centralizado, o que num monitor grande virava
  uma coluna estreita no meio da tela — queixa direta do usuário. Separar contêiner de linha de
  leitura permite alargar um sem alargar o outro.
- **Trade-off:** duas classes globais num projeto que até aqui só usava utilitário inline.
  Em compensação a largura deixa de ser decidida em oito arquivos diferentes.
- **Impacto:** `design-system.test.ts` falha se `max-w-5xl` voltar a aparecer em `src/`.

### AD-010: Prosa é posicionamento; lista de tecnologias é fato
- **Decisão:** o texto corrido do site (metadata, hero, abertura do About, contato) não nomeia
  linguagem nem framework, e não rotula o autor por recorte ("backend"). O fato técnico vive
  na lista Core/Also do About, nas tags dos projetos e no rodapé.
- **Razão:** o usuário busca oportunidades em qualquer linguagem, e o texto anterior
  ("Backend engineer working in C# and .NET", "I maintain and build .NET services",
  "Open to backend roles and to collaborating on .NET work") fazia um recrutador de outra
  stack se autoexcluir. Apagar o `.NET` de tudo seria o erro oposto: a página ficaria vaga e
  o candidato, sem quilometragem demonstrável.
- **Trade-off:** a página perde o match óbvio e imediato para quem procura especificamente
  .NET — agora esse leitor precisa chegar até a lista Core. Aceito conscientemente pelo
  usuário (alcance > match imediato).
- **Impacto:** `design-system.test.ts` → `PS-01` falha nos dois sentidos: se `C#`/`.NET`/
  `ASP.NET`/`backend` voltarem à prosa, **e** se o `.NET` sumir da lista Core.

### AD-011: Ícone do site em `app/icon.svg`, pintado por classe e não por custom property
- **Decisão:** o favicon padrão do Next sai do repositório; entra `src/app/icon.svg` (o
  brackets ⟨P⟩), com as cores em seletor de classe sob `@media (prefers-color-scheme)`.
- **Razão:** o SVG original do usuário usava `var(--bg)`/`var(--fg)`. librsvg 2.62 — motor SVG
  do ImageMagick e de vários crawlers e geradores de preview — não resolve custom property e
  renderizava o ícone como quadrado preto sólido, sem o P. Verificado por experimento com três
  SVGs mínimos, não por memória.
- **Trade-off:** o `.svg` fica um pouco mais verboso que a versão com tokens. Em troca a mesma
  arte funciona no navegador e em qualquer rasterizador.
- **Impacto:** `design-system.test.ts` → `PS-02` exige `icon.svg` presente, `favicon.ico`
  ausente, e o bloco `prefers-color-scheme` com o `aria-label` preservado.

### AD-012: Portfólio passa a ter área administrativa autenticada
- **Decisão:** revogar a linha de `PROJECT.md` que colocava "área administrativa / CRUD
  autenticado de projetos" **fora de escopo de nível projeto**. Passa a existir `/admin` no
  frontend e escrita autenticada em `/api/projects`.
- **Razão:** cadastrar projeto era digitar sete campos no console do Firebase, três deles por
  decisão dentro de um array de mapas. O próprio AD-008 registrou "dobra o custo de digitação no
  console, sem painel admin" como custo assumido — esta feature paga essa dívida. Pedido do
  usuário em 2026-08-24.
- **Trade-off:** a superfície pública cresce em quatro endpoints e uma rota. O endpoint mais
  perigoso (`DELETE`) passa a existir na internet, protegido por JWT e por confirmação digitada.
  Um dos dois projetos semeados pelo `DataSeeder` anuncia "No admin area" como decisão — o texto
  do seed ficou desatualizado de propósito, não é dado de produção.
- **Impacto:** `PROJECT.md`, `ROADMAP.md`, `CONVENTIONS.md` e `INTEGRATIONS.md` atualizados.
  A ideia adiada "Endpoints de escrita / painel admin" (2026-08-18) sai do backlog.

### AD-013: Autenticação por senha + JWT emitido pela própria API
- **Decisão:** senha única guardada como hash PBKDF2 em `Admin__PasswordHash`; login devolve JWT
  HS256 de 8h assinado com `Admin__JwtSigningKey`. Recusadas: Firebase Authentication e chave
  estática em header.
- **Razão:** há um único administrador — cadastro, perfis e recuperação de senha não têm para
  quem existir. O JWT é stateless, o que importa porque a instância do Render dorme e sobe
  zerada: sessão em memória exigiria login novo a cada religamento. `JwtBearer` nativo também é
  peça demonstrável, que é objetivo declarado do projeto.
- **Trade-off:** não existe logout do lado do servidor — token vazado vale até expirar. A
  revogação de emergência é trocar `Admin__JwtSigningKey`, que invalida todos de uma vez. O
  token vive em `sessionStorage` e não em cookie httpOnly, porque painel (Vercel) e API (Render)
  são sites diferentes e o cookie seria third-party, barrado pelo Safari e pelo Chrome — o
  preço é exposição a XSS, mitigada por `/admin` só renderizar texto do próprio admin.
- **Impacto:** duas variáveis de ambiente novas no Render. Sem elas a área fica **fechada**
  (login 401, escrita 401), o oposto do `EmailService`, que degrada em silêncio (C-9).

### AD-014: A escrita reaproveita o model, e o servidor descarta `id` e `createdAt` da entrada
- **Decisão:** seguir sem DTO (C-4): `POST`/`PUT` desserializam `Project` inteiro e o servidor
  ignora `id` e `createdAt` vindos do cliente. `PUT` lê o documento antes de gravar para
  preservar a data de criação.
- **Razão:** `CONVENTIONS.md` fixa "sem DTOs", e introduzi-los seria mudança de arquitetura
  fora do pedido. A leitura antes da escrita não é zelo: `SetAsync` substitui o documento
  inteiro, então sem ela `createdAt`, `tags` e `decisions` ausentes no corpo voltariam a vazio.
- **Trade-off:** o compilador não expressa "estes dois campos são ignorados" — quem segura essa
  regra é `AdminProjectsEndpointTests.Post_ignora_id_e_createdAt_enviados_pelo_cliente`. E cada
  edição custa duas idas ao Firestore em vez de uma.
- **Impacto:** o contrato público de leitura não muda em nada (ADM-12 é o teste de regressão).

### AD-015: rate limit do contato encadeia janela por visitante e teto do endpoint
- **Decisão:** `POST /api/contact` passa a ter duas camadas — 3/min por IP e 20/min no endpoint
  inteiro —, encadeadas com `PartitionedRateLimiter.CreateChained` e registradas como
  `GlobalLimiter`. O IP vem do `X-Forwarded-For` via `UseForwardedHeaders` com as listas de proxy
  limpas e `ForwardLimit = 1`. `RejectionStatusCode` global vira `429`.
- **Razão:** a partição única fazia três requisições de um script deixarem o formulário
  indisponível para todos (C-6). Particionar por IP sozinho abriria buraco novo: quem alcançar a
  aplicação sem passar pelo proxy escreve o cabeçalho que quiser e ganha partição infinita — daí
  o teto. O encadeamento é registrado como `GlobalLimiter`, e não como política do endpoint,
  porque com o teto adquirido primeiro **uma recusa por visitante consome 2 permits do teto**
  (medido), e um atacante sozinho reproduziria o C-6 em ~11 requisições.
- **Trade-off:** o endpoint deixa de declarar `.RequireRateLimiting` na própria linha — quem lê
  `ContactEndpoints.cs` só encontra o limite por um comentário e pela constante `Rota`. E o teto
  de 20/min ainda pode ser esgotado por quem forja cabeçalho: o C-6 sobe a barra de 3 para 20,
  não desaparece. Escolhido pelo usuário em 2026-08-26 entre três opções.
- **Impacto:** revoga a premissa da D-10 do `admin-panel` (particionar por `X-Forwarded-For` é
  falso isolamento) — vale para o endereço à esquerda, não para o à direita. O login **não** mudou.

## Bloqueios (B-NNN)

### B-002: Service account do Firebase dentro do repositório — RESOLVIDO
- **Descrição:** o arquivo `portfoliodb-9215c-firebase-adminsdk-fbsvc-d864c5e256.json`
  (projeto `portfoliodb-9215c`) foi colocado em `PortfolioApi/`. Chave privada real.
- **Auditoria (2026-08-18):** nunca foi commitada. `git rev-list --all` + `git grep` não
  encontram `BEGIN PRIVATE KEY` nem `private_key` em nenhum commit. **Sem vazamento;
  rotação desnecessária.**
- **Contorno atual:** `.gitignore:8-14` ignora o formato (`*firebase-adminsdk*.json` e afins),
  não apenas este arquivo — chave nova ou renomeada continua protegida.
- **Resolvido (2026-08-18):** chave movida para `~/.config/gcloud/` com permissão 600,
  fora da árvore do repositório. Ver L-006 para o risco descoberto no caminho.

### B-003: `appsettings.json` local defasado em duas frentes — RESOLVIDO
- **Descrição:** o arquivo local (gitignored) ainda tem `ConnectionStrings:DefaultConnection`
  do Postgres, **não tem seção `Firestore`**, e a seção `Email` está no formato SMTP antigo
  (`Host`, `Port`, `Username`, `Password`) enquanto `EmailService` lê `ApiKey`/`FromAddress`/
  `ToAddress` (Resend) desde o commit b1d321b.
- **Efeito:** localmente o Firestore não conecta (falta `ProjectId`) e `IsConfigured` é false,
  então o contato degrada silenciosamente sem enviar e-mail.
- **Resolvido (2026-08-18):** `Firestore:ProjectId` adicionado; `ConnectionStrings` morto
  removido; seção `Email` convertida para o formato Resend (`ApiKey`/`FromAddress`/`ToAddress`),
  com as 4 chaves SMTP mortas eliminadas — inclusive a senha de app do Gmail, que já não era
  lida por nenhum código. App verificada subindo com a config nova.
- **Confirmado em produção (2026-08-18):** o usuário recebeu o e-mail de teste disparado pelo
  formulário — logo `Email__ApiKey` está corretamente configurada no Render. `Email:ApiKey`
  local segue vazia por escolha; afeta só o envio em desenvolvimento.
- **Ação do usuário:** a senha de app do Gmail não existe mais em disco, mas segue válida na
  conta Google; o usuário assumiu a revogação em 2026-08-18 — passo fora do repositório,
  não verificável a partir daqui.

## Lições (L-NNN)

### L-001: Duas camadas de deprecação na credencial do Google Cloud
`FirestoreDbBuilder.JsonCredentials` **e** `GoogleCredential.FromJson` estão ambos
`[Obsolete]` por risco de segurança. O caminho atual é
`CredentialFactory.FromJson<ServiceAccountCredential>(json)` +
`GoogleCredential.FromServiceAccountCredential(...)`. Nenhuma das duas deprecações era
adivinhável — só apareceram ao compilar de verdade contra o assembly.

### L-002: Firestore rejeita DateTime que não seja Kind=Utc
`ArgumentException: Conversion from DateTime to Timestamp requires the DateTime kind to
be Utc`. Normalizar no model evita a exceção em runtime. Verificado no emulador.

### L-003: Emulador Firestore exige JDK 21+
`firebase-tools` recusa Java < 21. A máquina tem JDK 17 como padrão e JDK 25 em
`/usr/lib/jvm/java-25-openjdk` — apontar `JAVA_HOME` para o 25 antes de subir o emulador.

### L-004: `ConvertTo<T>()` já devolve null para documento inexistente
Verificado no emulador: `Exists=False` → `ConvertTo<Project>()` retorna `null` (tipo de
referência). A checagem explícita de `snapshot.Exists` em `FirestoreProjectStore` é portanto
redundante — mantida por explicitar a intenção. Consequência prática: no teste de mutação, a
remoção dessa checagem é **mutação equivalente**, não lacuna de cobertura.

### L-005: `EmulatorDetection` padrão é `None`
`new FirestoreDbBuilder()` ignora `FIRESTORE_EMULATOR_HOST` por padrão. Sem definir
`EmulatorDetection.EmulatorOrProduction`, rodar a API local contra o emulador falha silenciosamente
indo para produção. Verificado por reflection.

### L-006: o SDK Web do .NET assa qualquer `.json` do projeto dentro da imagem
`Microsoft.NET.Sdk.Web` trata `**/*.json` como Content e copia para a saída do `publish`.
Com a service account em `PortfolioApi/`, um `docker build` local a colocou em `/app/` dentro
da imagem — junto com o `appsettings.json` e a senha do Gmail. Verificado inspecionando a
imagem construída.

Produção nunca foi afetada: o Render constrói a partir do clone do git, onde ambos estão
ignorados, e lê a credencial de `Firestore__CredentialsJson`. O `.gitignore` sozinho **não**
protege builds locais, porque o Docker lê do disco, não do git — daí o `PortfolioApi/.dockerignore`.

### L-007: custom property é resolvida onde é DECLARADA, não onde é usada
`next/font` expõe `--font-newsreader` numa classe que estava no `<body>`, enquanto os tokens do
tema são declarados em `:root`. `--font-body: var(--font-newsreader), ...` em `:root` referencia um
var que não existe naquele escopo → o token inteiro vira *invalid at computed-value time* → o
`font-family` do body é descartado e a prosa cai no sans herdado do `html`.

O que torna isso perigoso: **build, lint e testes ficam todos verdes**. Só aparece no pixel. Foi
pego por screenshot, não por gate. Correção: as variáveis do `next/font` vão no `<html>`.
Guardado por um teste de regressão em `design-system.test.ts`.

### L-008: o Firestore trunca `DateTime` em microssegundo
`DateTime.UtcNow` tem precisão de 100 ns; o `Timestamp` do Firestore guarda microssegundos.
Gravar `...5523137Z` e ler de volta devolve `...5523130Z`. Comparação de igualdade exata entre
o objeto que foi enviado e o que foi lido **falha**, e o teste que compara data precisa usar o
valor gravado como referência. Descoberto quando o teste de preservação do `createdAt` ficou
vermelho por 7 ticks.

### L-009: o TestServer não aplica limite de corpo
`WebApplicationFactory` sobe a aplicação num TestServer em memória, que ignora
`IHttpMaxRequestBodySizeFeature`. Um corpo acima do limite chega inteiro e quem responde é a
validação (`400`), não o servidor (`413`). Contra Kestrel real o `413` acontece — verificado.
Consequência prática: requisito sobre tamanho de corpo não é observável por teste de integração
em memória; o que dá para afirmar automaticamente é o metadata declarado pelo endpoint.

### L-010: o lint do Next 16 reprova `setState` síncrono dentro de `useEffect`
`react-hooks/set-state-in-effect` (React Compiler) quebra o gate, e a análise segue para dentro
da função chamada pelo efeito — `void carregar()` também é acusado. Os dois caminhos que passam:
`useSyncExternalStore` para estado que vem de fora do React (foi o caso do `sessionStorage`), e
`setState` dentro do callback da promessa (`.then`), não no corpo do efeito.

### L-011: teste que não pode falhar é pior que teste ausente
CT-01..CT-03 exigiam "não deve chamar o Resend". Com `Email:ApiKey` vazio o `EmailService`
degrada em silêncio (`EmailService.cs:25`) e **nunca** chamaria nada — a asserção passaria
verde para sempre, inclusive com a validação removida. A correção foi configurar o e-mail no
teste e trocar o handler HTTP por um espião
(`ConfigureHttpClientDefaults` → `ConfigurePrimaryHttpMessageHandler`), que conta chamadas e
não toca a rede; verificado por protótipo antes de entrar. Regra que fica: quando o requisito é
"não faz X", verifique primeiro se o teste conseguiria ver X acontecendo.

### L-012: `ConfigureHttpClientDefaults` alcança o cliente sem nome do `IHttpClientFactory`
`services.ConfigureHttpClientDefaults(b => b.ConfigurePrimaryHttpMessageHandler(...))` vale
também para `CreateClient()` sem nome, que é o que o `EmailService` usa. Dentro de um
`WebApplicationFactory` isso vai em `builder.ConfigureTestServices(...)`, que roda depois das
registrações do `Program.cs` e por isso ganha. É o ponto de interceptação de qualquer chamada
HTTP de saída da API em teste, sem precisar injetar abstração nova no código de produção.

### L-013: no rate limiter, requisição recusada pode consumir permit duas vezes
Com o teto em `RateLimiterOptions.GlobalLimiter` e a partição por IP como política do endpoint, o
middleware adquire o global primeiro. Requisição **aceita** consome 1 permit do teto;
**recusada** pela política do endpoint consome **2** (medido: teto de 20 caiu 19→17→15→13→11 com
uma aceita e quatro recusadas). Consequência: encadear na ordem errada faz o limitador de trás
ser drenado justamente por quem já está sendo barrado. `PartitionedRateLimiter.CreateChained`
registrado como `GlobalLimiter` adquire na ordem declarada e não tem esse efeito — verificado.

### L-014: `UseForwardedHeaders` sem configuração é inerte, e o valor confiável é o da direita
Dois defaults desligam o middleware: `ForwardedHeaders = None` (nenhum cabeçalho é processado) e
`KnownProxies = [::1]` / `KnownNetworks = [127.0.0.1/8]` (só confia em proxy no loopback). Atrás
do Render, chamar `app.UseForwardedHeaders()` sem `Configure<ForwardedHeadersOptions>` não muda
nada e o `RemoteIpAddress` continua sendo o do proxy — sem erro, sem log.

Com as listas limpas, o middleware consome a entrada **mais à direita** do `X-Forwarded-For`, que
é a que o proxy imediato anexou — e é por isso que particionar por ela não é falso isolamento.
`ForwardLimit = 1` (o padrão) é a garantia: com `2` ele anda um salto à esquerda e passa a
acreditar no valor escrito pelo cliente. Valor malformado não vira partição lixo — o IP
simplesmente permanece o da conexão.

## Ideias Adiadas

- **A lista Core do About continua sendo o sinal `.NET` mais forte da página.** Depois do
  AD-010 a prosa é neutra, mas o bloco Core é `C# · .NET · ASP.NET Core · PostgreSQL ·
  Firestore · Docker` — 4 de 6 são .NET, e é o primeiro bloco visual que um recrutador
  escaneia. O usuário decidiu explicitamente não mexer nele nesta tarefa. Se um dia quiser
  ir além, a forma seria agrupar por eixo (linguagens / dados / infra) em vez de por
  profundidade. (2026-08-20)

- **Ícone só em SVG.** Não existe `apple-icon`, `manifest.json` nem open-graph image, e não há
  `.ico` multi-tamanho — a 16px o brackets fica macio por desenho, e um `.ico` rasterizado à
  mão em 16/32/48 daria mais controle de hinting. Só vale a pena se o borrão incomodar no ar.
  (2026-08-20)

- **Vulnerabilidades restantes em devDependencies.** Depois do bump para Next 16.3.1,
  `npm audit --omit=dev` dá zero, mas `npm audit` completo ainda aponta 4 achados
  (`postcss <=8.5.22` via `@tailwindcss/postcss` e `vitest`; `brace-expansion` e `js-yaml`
  via a cadeia do eslint). Não vão para o bundle do visitante — o risco é limitado à máquina
  de quem builda. Se for tratar, é `npm audit fix` sem `--force` e gate completo depois.
  (2026-08-18)

- **Introduzir DTOs na API.** Hoje os models são serializados direto (C-4), então mudar
  um campo quebra o contrato público sem aviso do compilador. (2026-08-18)
- ~~**Endpoints de escrita / painel admin**~~ — **feito** em 2026-08-24, feature
  `admin-panel` (AD-012).

- **Revalidação sob demanda do ISR ao salvar no painel.** Hoje a home leva até 60s para refletir
  uma mudança (`next: { revalidate: 60 }`). Um `revalidatePath` disparado pelo painel deixaria a
  atualização instantânea; exige rota de revalidação no Next protegida por segredo. Deixado fora
  do escopo da feature do painel. (2026-08-24)

- **Semente desatualizada.** `DataSeeder` semeia um projeto cuja decisão declarada é
  "No admin area" — texto que a feature `admin-panel` tornou falso. Só afeta banco vazio, e os
  documentos de produção não vêm do seed. (2026-08-24)

- ~~**`CLAUDE.md:23` afirma "No test runner is configured in `package.json`"**~~ — **feito** em
  2026-08-24, no fechamento da feature `admin-panel`: a linha passou a descrever o vitest e o
  jsdom por docblock, que a feature tornou parte do gate.

- **`Retry-After` na resposta 429.** O `FixedWindowRateLimiter` não emite a metadata (verificado
  no C-6), então o cliente não sabe quanto falta para a janela virar. Produzir o cabeçalho exige
  calcular o fim da janela à mão no `OnRejected`. (2026-08-26)

- **Rate limit do login por visitante.** O C-6 particionou só o contato; `LoginRateLimitPolicy`
  segue com partição única, então alguém pode esgotar as 5 tentativas/min e deixar o admin um
  minuto sem entrar. A infraestrutura já existe — é aplicar a mesma chave de partição. Ficou de
  fora por disciplina de escopo: o C-6 é sobre o endpoint de contato. (2026-08-26)

- **`OnRejected` redundante em `LoginRateLimitPolicy`.** Desde que o `RejectionStatusCode` global
  virou 429 (AD-015), o `OnRejected` da política do login não muda mais nada. Removê-lo é limpeza
  de duas linhas em arquivo de autenticação, fora do escopo do C-6. (2026-08-26)
