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

## C-5: `POST /api/contact` aceita entrada sem limite nem formato — ALTO
Os `[Required]` de `Models/ContactMessage.cs:7-12` são decorativos: Minimal API do .NET 9 não
executa DataAnnotations automaticamente. A validação real é a checagem manual de
`Endpoints/ContactEndpoints.cs:12-14`, que só rejeita Email e Message vazios. Não há teto de
tamanho (o Kestrel aceita 30 MB por padrão), nem validação de formato de e-mail, nem qualquer
checagem de `Name` — que vai direto para o *subject* enviado ao Resend.

## C-6: Rate limit é global, não por visitante — MÉDIO
`AddFixedWindowLimiter("contact", ...)` em `Program.cs:64-70` cria **uma única partição**: os
3 req/min valem para o endpoint inteiro somando todos os visitantes. Três requisições de um
script deixam o formulário indisponível para todo mundo até a janela virar. Particionar por IP
não resolve sozinho: falta `UseForwardedHeaders`, então atrás do proxy do Render o
`RemoteIpAddress` é o proxy, não o visitante — as duas mudanças andam juntas.

## C-7: Container roda como root — MÉDIO
`PortfolioApi/Dockerfile:8-13` não define `USER $APP_UID`. A imagem final também carrega
`iproute2`, ferramenta de diagnóstico de rede sem uso em runtime.

## C-8: Frontend sem headers de segurança — MÉDIO
`frontend/next.config.ts` está vazio e a Vercel não injeta nada por padrão: sem CSP, sem
`X-Frame-Options`/`frame-ancestors` (o site é clickjackável), sem `X-Content-Type-Options`,
sem `Referrer-Policy`.

## C-9: Envio de e-mail degrada em silêncio — MÉDIO
`Services/EmailService.cs:25-31`: sem `Email:ApiKey` configurada, a API loga um warning e
responde `{success: true}` — o visitante vê "Message sent!" e a mensagem não existe em lugar
nenhum. É intencional (evita derrubar o endpoint por config faltando) e hoje produção está
configurada (ver B-003), mas qualquer perda futura da env var vira perda silenciosa de contato.

## C-10: Formulário sem anti-spam — MÉDIO
Não há honeypot nem captcha. A única barreira é o rate limit de C-6, que paga com
disponibilidade o que entrega em proteção.

## C-11: Achados de baixa severidade — BAIXO
- `PortfolioApi/appsettings.Development.json:10-11` versiona uma connection string de Postgres
  com senha. Config **morta** desde AD-001 — o Postgres não existe mais no projeto.
- `appsettings.Development.json:8` — `AllowedHosts: "*"`, sem validação de Host header.
- `Endpoints/ContactEndpoints.cs:16` loga o e-mail do remetente em nível Information (PII em log).
- `.github/workflows/keep-api-awake.yml:24-27` não declara bloco `permissions:`, então o
  `GITHUB_TOKEN` recebe as permissões default. O job só faz um `curl`; `permissions: {}` basta.
- Sem Dependabot e sem `npm audit`/`dotnet list --vulnerable` no CI — foi exatamente por isso
  que os CVEs do item 1 passaram despercebidos até a auditoria manual.

## C-12: Verificações fora do repositório — INDETERMINADO
Não são verificáveis a partir do código; exigem o console e ficam com o usuário:
- **Regras do Firestore.** O backend usa service account e ignora as rules, mas se o projeto
  estiver com regras default abertas, o banco fica acessível via SDK cliente/REST por quem
  souber o projectId. O esperado é `allow read, write: if false;`.
- **Papel da service account:** deve ser *Cloud Datastore User*, nunca Editor/Owner.
- **Chave do Resend:** restrita a envio e rotacionada se já circulou fora do gerenciador de
  segredos (a auditoria não a encontrou no histórico do git).
