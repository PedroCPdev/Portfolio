# ARCHITECTURE.md

## Fluxo de dados

```
Visitante → Vercel (Next.js server component)
              └─ lib/api.ts  fetch ISR 60s, 3 tentativas (~54s) em falha de rede
                   → Render (ASP.NET Core Minimal API)
                        ├─ GET  /api/projects       → FirestoreProjectStore → Cloud Firestore
                        ├─ GET  /api/projects/{id}  → FirestoreProjectStore → Cloud Firestore
                        ├─ POST /api/contact        → ValidationFilter → EmailService → Resend
                        │                             (3/min por visitante + 20/min no endpoint,
                        │                              429 no excedente, corpo ≤ 32 KB)
                        └─ GET  /health             → responde sem tocar no Firestore
                                                      ↑
cron-job.org (a cada 10 min das 08:00 às 23:50 BRT, AD-016) ────────────────┘

Admin (browser) → Vercel /admin  (client component, sem ISR, sessionStorage)
              └─ lib/admin.ts  fetch cache: "no-store", Authorization: Bearer
                   → Render
                        ├─ POST   /api/admin/login  → AdminPasswordHasher + AdminTokenService
                        │                             (rate limit 5/min, 429)
                        ├─ POST   /api/projects     → ValidationFilter → FirestoreProjectStore
                        ├─ PUT    /api/projects/{id}→ ValidationFilter → FirestoreProjectStore
                        └─ DELETE /api/projects/{id}→ FirestoreProjectStore
                          (as três exigem JWT válido; corpo limitado a 64 KB)
```

## Camadas da API
1. **Endpoints/** — validação de entrada e shape da resposta. Sem lógica de negócio.
2. **Data/FirestoreProjectStore** — única peça que fala Firestore. Injetada nos endpoints,
   ocupando o lugar que o `AppDbContext` ocupava (mesma convenção: uma peça de dados injetada).
   Também é quem traduz `RpcException` em `bool`: gRPC não sobe para o endpoint.
3. **Services/** — EmailService (Resend), DataSeeder (seed idempotente no startup).
4. **Auth/** — `AdminOptions` (configuração e o `IsConfigured` que fecha a área quando falta
   variável), `AdminPasswordHasher` (PBKDF2), `AdminTokenService` (emissão e parâmetros de
   validação do JWT) e `LoginRateLimitPolicy`.
5. **Validation/** — `ValidationFilter<T>` roda as DataAnnotations do corpo antes do endpoint,
   porque Minimal API não as executa sozinha; `ValidateItemsAttribute` desce nos itens de
   coleção, que o `Validator` também não faz; `NotBlankAttribute` recusa string só de espaço.

A escrita é ler-e-gravar por necessidade: `SetAsync` substitui o documento inteiro, então o
`PUT` lê antes para preservar `createdAt` e para distinguir id inexistente (404) de id existente.

`/health` (`Endpoints/HealthEndpoints.cs`) foge dessa divisão de propósito: não passa por store
nem por serviço, porque existe só para o keep-alive acordar a instância sem gastar cota de
leitura do Firestore (AD-006). Fica fora do OpenAPI via `ExcludeFromDescription()`.

## Program.cs — 3 fases
1. **Services**: OpenAPI, ProblemDetails, CORS (`"Frontend"`), `FirestoreDb` singleton,
   `FirestoreProjectStore`, EmailOptions/EmailService, ForwardedHeaders (X-Forwarded-For, listas
   de proxy limpas) e RateLimiter (`GlobalLimiter` = encadeado do contato; política
   `"admin-login"`; `RejectionStatusCode` 429).
2. **Seed**: `DataSeeder.SeedAsync` — idempotente, só popula se a coleção estiver vazia.
   Falha de seed **não derruba a aplicação** (ver AD-003).
3. **Pipeline + Endpoints**: Scalar em `/scalar` só em Development; `UseForwardedHeaders` antes de
   tudo (o rate limit particiona por `RemoteIpAddress`); CORS; RateLimiter; endpoints
   (`MapProjectsEndpoints`, `MapContactEndpoints`, `MapHealthEndpoints`).

## Decisão-chave: sem migrations
Firestore é schemaless. A FASE 2 antiga (`db.Database.MigrateAsync()`) deixou de existir —
com ela sai a causa raiz do crash de startup quando o banco estava pausado.

## Resiliência ao cold start
O free tier do Render desliga a instância após ~15 min sem tráfego e leva ~1 min para religar.
Duas camadas independentes tratam isso, nenhuma delas no caminho do banco:
1. **Keep-alive** — o cron-job.org pinga `/health` a cada 10 min numa janela de 16h/dia (AD-016).
   Mecanismo detalhado em `INTEGRATIONS.md`; razão da janela em AD-005.
2. **Retry no frontend** — `getProjects()` (`frontend/src/lib/api.ts:17-42`) tenta 3 vezes com
   timeout de 15s e backoff de 3s e 6s, cobrindo ~54s. Erro HTTP 4xx/5xx **não** é repetido
   (não melhora com insistência); só timeout e falha de rede. Esgotadas as tentativas, retorna
   `[]` em vez de lançar, para não quebrar o `next build`.

## Referência detalhada

Migrada do `CLAUDE.md` em 2026-10-19, sem alteração de conteúdo.

### API (`PortfolioApi/`)
Minimal API style, no controllers. `Program.cs` is organized in three commented phases:
1. **Services** — DI registrations (OpenAPI, CORS policy named `"Frontend"` sourced from `AllowedOrigins` config, a `FirestoreDb` singleton and `FirestoreProjectStore`)
2. **Seed** — runs automatically on every startup, not just in Development; wrapped in try/catch that logs and continues
3. **Pipeline + Endpoints** — Scalar docs mounted at `/scalar` only in Development; endpoints registered via extension methods

Endpoints are grouped in `Endpoints/` as static classes with `Map*Endpoints(this WebApplication app)` extension methods (`ProjectsEndpoints`, `ContactEndpoints`, `HealthEndpoints`, `AdminEndpoints`), each mapping its own route group. `GET /health` is the odd one out: it returns `{"status":"ok"}` without touching Firestore and is excluded from OpenAPI — it exists purely as the keep-alive ping target, so the ping costs no Firestore read quota. `Data/FirestoreProjectStore.cs` is the only place that talks to Firestore, injected into endpoints the way `AppDbContext` used to be. `Services/DataSeeder.cs` seeds two example `Project` documents idempotently.

Firestore document ids are **strings** (20 chars), so `Project.Id` is a `string` and the route is `/{id}`, not `/{id:int}`. `Project.CreatedAt` normalizes any `DateTimeKind` to UTC in its setter — Firestore throws `ArgumentException` on non-UTC `DateTime`. `Services/EmailService.cs` sends through the Resend HTTP API; `POST /api/contact` runs `ValidationFilter<ContactMessage>` over the body (name ≤ 80, email ≤ 254 and well-formed, message ≤ 4000 — rejected with `{ success: false, error }`, the shape the frontend reads), caps the body at 32 KB, then sends. Rate limiting is two chained layers registered as the `GlobalLimiter` (`RateLimiting/ContactRateLimiter.cs`), not an endpoint policy: 3/min per visitor and 20/min for the endpoint as a whole, rejected with `429`. The order is the point — with the ceiling acquired first, a request the per-visitor window rejects burns **two** of its permits, so one attacker could drain it. The visitor's IP comes from `X-Forwarded-For` via `UseForwardedHeaders`, which is **inert unless configured** (`ForwardedHeaders` defaults to `None` and only loopback proxies are trusted); `ForwardLimit = 1` is what makes it read the rightmost entry, the one the proxy appended, instead of what the client wrote. When the `Email` section is not configured (`IsConfigured` false) it degrades silently — it logs and reports success without sending.

`POST`, `PUT` and `DELETE` on `/api/projects` require an admin JWT (`RequireAuthorization()`), run `ValidationFilter<Project>` over the body and cap it at 64 KB; `GET` stays anonymous. `POST /api/admin/login` checks the password against the PBKDF2 hash in `Admin:PasswordHash` and issues an 8h HS256 token signed with `Admin:JwtSigningKey` — **when either is missing or the key is under 32 bytes the whole admin area is closed** (login 401, writes 401), the opposite of `EmailService`'s silent degradation. Generate the hash with `dotnet run -- hash-password <password>`. `PUT` reads before writing because Firestore's `SetAsync` replaces the whole document, which would otherwise wipe `createdAt`; `DELETE` uses `Precondition.MustExist` because deleting a missing document does **not** throw.

`Models/` are plain classes with no separate DTOs — API responses serialize them directly (see `Project`, `ContactMessage`). `Project.Decisions` (JSON `decisions`) is an ordered list of `Decision` (`title`/`why`/`cost`) that feeds the project detail page; the order is meaningful, it is the sequence the reader should follow. It defaults to an **empty array, never null**: Firestore is schemaless, so documents written before the field existed simply lack the key, and consumers `.map` over it without a null check. `Project` carries Firestore mapping attributes (`[FirestoreData]`, `[FirestoreProperty]`, `[FirestoreDocumentId]`); `ContactMessage` is never persisted.

### Frontend (`frontend/`)
Next.js App Router, three routes. `src/app/page.tsx` composes section components in fixed order (`Navbar`, `Hero`, `About`, `Projects`, `Contact`), anchored via `id` for in-page nav — it is a **showcase only** and deliberately renders no decision or cost. `src/app/projects/[id]/page.tsx` is the detail page: it holds the reasoning, one `DecisionRecord` per decision, and is where the trade-off ledger lives. `generateStaticParams` pre-renders the known projects at build; because `getProjects` swallows failures and returns `[]`, an API that is down yields no params instead of a failed build, and those ids fall back to on-demand rendering. An unknown id calls `notFound()`.

Layout width is owned by two classes in `globals.css`, not by per-component `max-w-*`: `.shell` is the page container (96rem cap, padding that grows with the viewport) and `.measure` caps prose at 65ch. They are separate on purpose — a wide screen should widen the container, never the reading line. `design-system.test.ts` fails if `max-w-5xl` reappears.

`src/app/admin/` is the admin panel: a client component tree (`src/components/admin/`) that signs in, lists, creates, edits and deletes projects, with a decision editor whose order is what gets saved. It talks to the API through `src/lib/admin.ts` — a **separate** boundary from `api.ts`, with `cache: "no-store"`, an `Authorization` header and a discriminated result that surfaces errors instead of swallowing them. The session (a JWT) lives in `sessionStorage`, read through `useSyncExternalStore`; the route declares `robots: { index: false }` and nothing public links to it (`design-system.test.ts` guards both). Saving does not force a revalidation — the home page reflects a change within its 60s ISR window.

`src/lib/api.ts` is the sole data-fetching boundary for the public site — typed `fetch` helpers against the API, using Next's `next: { revalidate: 60 }` for ISR. `Projects` (`src/components/Projects.tsx`) is an async server component that calls `getProjects()` directly; on fetch failure `getProjects` swallows the error and returns `[]`, and the component renders an empty-state message rather than an error. Because Render's free tier sleeps, `getProjects` retries network/timeout failures 3 times (15s each, 3s and 6s backoff, ~54s total) before giving up — HTTP 4xx/5xx is not retried. Budget for that when a call runs with the API down.

Styling is Tailwind v4 over a token system declared in `src/app/globals.css` — dark olive-graphite ground (`--ground`), signal amber accent (`--amber`), and `--clay` reserved for trade-off cost lines and errors. Use the generated utilities (`bg-ground`, `text-amber`, `border-rule`); a literal hex in a component fails `src/__tests__/design-system.test.ts`. Type roles are fixed: `font-display` (Bricolage Grotesque) for headlines and titles, `font-body` (Newsreader) for prose, `font-mono` (JetBrains Mono) for ids, labels and tags.

`next/font` variables live on `<html>`, not `<body>` — the theme tokens are declared on `:root`, and a custom property resolves where it is *declared*. Putting the font variables on `<body>` makes `--font-body` reference a `--font-newsreader` that does not exist at `:root`, so it becomes invalid and prose silently falls back to the default sans, with build, lint and tests all still green.

### Cross-cutting
CORS is locked to a single allowed origin read from API config (`AllowedOrigins`) — update both `appsettings.json`/environment config and the frontend's `NEXT_PUBLIC_API_URL` together when changing where either side is hosted.

Firestore credentials come from `Firestore:CredentialsJson` (the whole service account JSON, as
`Firestore__CredentialsJson` env var on Render — no credential file on disk). When empty, the app
falls back to Application Default Credentials. `EmulatorDetection.EmulatorOrProduction` is set, so
`FIRESTORE_EMULATOR_HOST` routes to a local emulator when present.

A cron-job.org job pings `/health` every 10 minutes between 08:00 and 23:50 BRT to keep the Render instance awake (AD-016); `.github/workflows/keep-api-awake.yml` remains only as a manual ping. The window is deliberately not 24/7: Render grants 750 instance-hours/month and exceeding it suspends every free service until the next month. See `.specs/codebase/INTEGRATIONS.md`.
