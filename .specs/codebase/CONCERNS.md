# CONCERNS.md

## C-1: Cold start do Render mascarado como "sem projetos" — MITIGADO (2026-08-18)
`getProjects()` (`frontend/src/lib/api.ts`) engole qualquer erro e retorna `[]`, e
`Projects.tsx` renderiza "no projects yet" — um cold start do Render (~50s no free tier) era
indistinguível de "banco vazio" para o visitante. Trocar o banco não resolveu isso.
Mitigação em duas camadas (feature `render-cold-start`): keep-alive por GitHub Actions das
08:00 às 00:00 BRT e retry com backoff cobrindo ~54s. **Resíduo:** fora dessa janela o serviço
ainda dorme; o primeiro visitante da madrugada paga o religamento, agora absorvido pelo retry
em vez de ver a seção vazia. Eliminar o resíduo exigiria plano pago ou mover a leitura para o Next.

## C-2: Cobertura de testes recente e estreita — MÉDIO
Até 2026-08-18 nenhum dos dois sub-projetos tinha test runner. Hoje existem
`PortfolioApi.Tests` (xUnit, contra o emulador Firestore) e `vitest` no frontend, mas a
cobertura se concentra no que as duas features tocaram: acesso ao Firestore, seed e o retry
de `getProjects`. Componentes React e `EmailService` seguem sem teste.

## C-3: `DateTime` não-UTC quebra a escrita no Firestore — MÉDIO
Verificado empiricamente: `ArgumentException: Conversion from DateTime to Timestamp requires
the DateTime kind to be Utc`. Todo `DateTime` gravado precisa de `DateTimeKind.Utc`.

## C-4: Sem DTOs
Models são serializados direto. Mudar um campo do model muda o contrato público da API
sem nenhum aviso do compilador.

---

# Auditoria de segurança — 2026-08-18

Varredura completa dos dois sub-projetos, Docker, workflow e histórico do git, pedida pelo
usuário. O backlog abaixo (C-5..C-12) está na **ordem de correção acordada**: do mais severo
para o menos. C-1..C-4 acima são anteriores e não vieram desta auditoria.

**O que a auditoria confirmou estar correto** (não é concern, é a linha de base a não regredir):
nenhum segredo no repositório nem no histórico — `git rev-list --all` só encontra o placeholder
`re_YOUR_RESEND_API_KEY`; credencial por env var (AD-004); Scalar/OpenAPI só em Development;
`UseExceptionHandler` sem stack trace em produção; superfície de 2 GETs e 1 POST, nenhum
endpoint de escrita; SDK tipado do Firestore sem concatenação de query; React escapando por
padrão, sem `dangerouslySetInnerHTML`, com `rel="noopener noreferrer"` em todo `target="_blank"`;
`dotnet list package --vulnerable --include-transitive` sem nenhum CVE.

**Item 1 do backlog (CVEs high nas dependências do frontend) foi RESOLVIDO** no commit `602d02b`
— Next 16.3.1, `npm audit --omit=dev` em zero. Ver a entrada de 2026-08-18 no `JOURNAL.md`.

**Item 2 (C-5, validação de entrada no contato) foi RESOLVIDO** em 2026-08-26 — ver o próprio
C-5 abaixo. **Item 3 (C-6) foi RESOLVIDO** em 2026-08-26. **Item 4 (C-7) foi RESOLVIDO** em
2026-10-19. C-8, C-13 e quase todo o C-11 também, no mesmo dia. **Abertos: C-9, C-10, o
`AllowedHosts` do C-11 e o C-12** (console).

## C-5: `POST /api/contact` aceita entrada sem limite nem formato — RESOLVIDO (2026-08-26)
Os `[Required]` de `Models/ContactMessage.cs:7-12` são decorativos: Minimal API do .NET 9 não
executa DataAnnotations automaticamente. A validação real é a checagem manual de
`Endpoints/ContactEndpoints.cs:12-14`, que só rejeita Email e Message vazios. Não há teto de
tamanho (o Kestrel aceita 30 MB por padrão), nem validação de formato de e-mail, nem qualquer
checagem de `Name` — que vai direto para o *subject* enviado ao Resend.

**Resolvido** pela feature `features/contact-input-validation`: `ContactMessage` declara
`[NotBlank]`, `[StringLength]` (80/254/4000) e `[EmailAddress]`, e quem os executa é o
`ValidationFilter<ContactMessage>` pendurado em `Endpoints/ContactEndpoints.cs:42`. O corpo tem
teto de 32 KB (`:43`), contra os 30 MB do padrão do Kestrel. A checagem manual de vazio saiu do
handler — havia virado código morto atrás do filtro. Cobertura: `ContactEndpointTests.cs`,
15 testes; o `413` tem evidência contra Kestrel real na pasta da feature.

## C-6: Rate limit é global, não por visitante — RESOLVIDO (2026-08-26)
`AddFixedWindowLimiter("contact", ...)` criava **uma única partição**: os 3 req/min valiam para o
endpoint inteiro somando todos os visitantes, e o excedente recebia `503`. Três requisições de um
script deixavam o formulário indisponível para todo mundo até a janela virar.

**Resolvido** pela feature `features/contact-rate-limit-por-visitante`: `UseForwardedHeaders`
configurado (`Program.cs:97-109`, `X-Forwarded-For`, `ForwardLimit = 1`, listas de proxy limpas)
faz o `RemoteIpAddress` ser o do visitante, e `RateLimiting/ContactRateLimiter.cs` encadeia
3/min por visitante com 20/min no endpoint inteiro. `RejectionStatusCode` global passou a `429`
(`Program.cs:116`). Cobertura: `ContactRateLimitTests.cs`, 8 testes (RL-01..RL-07).

Duas coisas que **não** foram fechadas junto, de propósito: o rate limit do login segue com
partição única (`Auth/LoginRateLimitPolicy.cs`), e não há `Retry-After` na resposta 429 — o fixed
window não emite a metadata. Ambas em Ideias Adiadas.

## C-7: Container roda como root — RESOLVIDO (2026-10-19)
`PortfolioApi/Dockerfile:8-13` não define `USER $APP_UID`. A imagem final também carrega
`iproute2`, ferramenta de diagnóstico de rede sem uso em runtime.

**Resolução** (`e3ef82d`, tarefa rápida 001): a imagem roda como `app` (UID 1654) via
`USER $APP_UID`. O `apt-get` de `netbase` e `iproute2` saiu: os dois serviam ao Npgsql, que
saiu no AD-001. Verificado com o container lendo e escrevendo no emulador do Firestore.

## C-8: Frontend sem headers de segurança — RESOLVIDO (2026-10-19)
`frontend/next.config.ts` está vazio e a Vercel não injeta nada por padrão: sem CSP, sem
`X-Frame-Options`/`frame-ancestors` (o site é clickjackável), sem `X-Content-Type-Options`,
sem `Referrer-Policy`.

**Resolução** (`b039f3a`): `next.config.ts` envia CSP, `X-Frame-Options: DENY`, `nosniff`,
`Referrer-Policy` e `Permissions-Policy` em todas as rotas, guardados por
`security-headers.test.ts`. `script-src` mantém `'unsafe-inline'` (AD-017). Verificado no
Chromium: `/`, `/admin` e `/projects/x` hidratam sem violação.

## C-9: Envio de e-mail degrada em silêncio — MÉDIO
`Services/EmailService.cs:25-31`: sem `Email:ApiKey` configurada, a API loga um warning e
responde `{success: true}` — o visitante vê "Message sent!" e a mensagem não existe em lugar
nenhum. É intencional (evita derrubar o endpoint por config faltando) e hoje produção está
configurada (ver B-003), mas qualquer perda futura da env var vira perda silenciosa de contato.

## C-10: Formulário sem anti-spam — MÉDIO
Não há honeypot nem captcha. A única barreira é o rate limit do C-6 — que desde 2026-08-26 já não
paga a disponibilidade de todos pela proteção, mas continua sem impedir robô educado: 3 mensagens
por minuto por IP são mais que suficientes para saturar a cota do Resend (100 e-mails/dia).

## C-11: Achados de baixa severidade — PARCIAL (2026-10-19)
- `PortfolioApi/appsettings.Development.json:10-11` versiona uma connection string de Postgres
  com senha. Config **morta** desde AD-001 — o Postgres não existe mais no projeto.
- `appsettings.Development.json:8` — `AllowedHosts: "*"`, sem validação de Host header.
- `Endpoints/ContactEndpoints.cs:16` loga o e-mail do remetente em nível Information (PII em log).
- `.github/workflows/keep-api-awake.yml:24-27` não declara bloco `permissions:`, então o
  `GITHUB_TOKEN` recebe as permissões default. O job só faz um `curl`; `permissions: {}` basta.
- Sem Dependabot e sem `npm audit`/`dotnet list --vulnerable` no CI — foi exatamente por isso
  que os CVEs do item 1 passaram despercebidos até a auditoria manual.

**Resolvidos em 2026-10-19:** connection string morta e e-mail no log (`d420813`); `permissions:`
nos dois workflows, Dependabot e `scripts/audit.mjs` no CI (`ed4998c`). **Aberto:** `AllowedHosts`
continua `*`; a correção é a env var `AllowedHosts` no Render com o host do serviço, fora do
repositório. **Aceito:** 5 avisos altos de `braces` em dev (via `eslint-config-next`), sem versão
corrigida publicada (faixa `<=3.0.3`); só roda no lint, com padrões de glob do próprio repo.

## C-12: Verificações fora do repositório — INDETERMINADO
Não são verificáveis a partir do código; exigem o console e ficam com o usuário:
- **Regras do Firestore.** O backend usa service account e ignora as rules, mas se o projeto
  estiver com regras default abertas, o banco fica acessível via SDK cliente/REST por quem
  souber o projectId. O esperado é `allow read, write: if false;`.
- **Papel da service account:** deve ser *Cloud Datastore User*, nunca Editor/Owner.
- **Chave do Resend:** restrita a envio e rotacionada se já circulou fora do gerenciador de
  segredos (a auditoria não a encontrou no histórico do git).

## C-13: `GET /api/projects/{id}` responde 500 para id com barra — RESOLVIDO (2026-10-19)
`Data/FirestoreProjectStore.cs:30` chama `Projects.Document(id)` sem validar o id.
`Document("a/b")` lança `ArgumentException: Path must have an odd number of elements`
(verificado no emulador, evidência da feature `admin-panel`), e um `%2F` na URL chega ao handler
já decodificado — a resposta vira `500` em vez de `404`.

Severidade baixa: não vaza dado, não escreve nada, e só quem monta a URL à mão chega lá. Vira
ruído em log e uma resposta errada de contrato.

Os métodos de **escrita** (`UpdateAsync`/`DeleteAsync`, `FirestoreProjectStore.cs:58` e `:78`)
já tratam id inválido como inexistente desde 2026-08-24; a correção do `GET` não entrou junto
por disciplina de escopo — a tarefa era o painel admin, não o endpoint de leitura. A correção é
mover a guarda `IdValido` (`FirestoreProjectStore.cs:97`) para `GetByIdAsync`, com um teste que
hoje ficaria vermelho.

**Resolução** (`7d213c7`): `GetByIdAsync` usa a guarda `IdValido`. Achado no caminho: id com duas
barras (`a/b/c`) não lançava, lia um documento de subcoleção. 3 testes novos.
