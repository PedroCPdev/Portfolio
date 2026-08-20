# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

Personal portfolio site: a Next.js frontend backed by an ASP.NET Core Minimal API, with Cloud Firestore storage. The frontend renders project data server-side with a 60-second ISR cache. Live at pedrocpdev.vercel.app; API deploys separately (Docker, see `PortfolioApi/Dockerfile`).

Two independent sub-projects live in this repo, each with its own dependency tree and toolchain — there is no shared root package.json/solution wiring them together:
- `frontend/` — Next.js 16, React 19, TypeScript, Tailwind CSS v4
- `PortfolioApi/` — ASP.NET Core 9 Minimal API, Google.Cloud.Firestore 4.4.0, Scalar for OpenAPI docs

## Commands

### Frontend (`frontend/`)
```bash
npm install       # first-time setup
npm run dev       # dev server at http://localhost:3000
npm run build     # production build
npm run start     # serve production build
npm run lint      # eslint
```
No test runner is configured in `package.json`.

### API (`PortfolioApi/`)
```bash
dotnet run                          # runs at http://localhost:5002; seed applies automatically on startup
dotnet build
```

### Tests (`PortfolioApi.Tests/`)
```bash
# Firestore emulator is required — firebase-tools needs JDK 21+
export JAVA_HOME=/usr/lib/jvm/java-25-openjdk
export PATH="$JAVA_HOME/bin:$PATH"
npx -y firebase-tools@latest emulators:start --only firestore --project demo-portfolio

export FIRESTORE_EMULATOR_HOST=127.0.0.1:8080
dotnet test PortfolioApi.Tests
```
Integration tests fail loudly (never skip) when the emulator is not running. Each test uses a
GUID-suffixed collection, so they are parallel-safe.

Requires `appsettings.json` in `PortfolioApi/` (gitignored) — copy `appsettings.Example.json` and fill in the `Firestore` section and `AllowedOrigins`. Firestore is schemaless, so there are no migrations; on startup the app runs `DataSeeder.SeedAsync()`, which only seeds if the `projects` collection is empty. A seed failure is logged and **does not** stop the app from starting.

Frontend needs `NEXT_PUBLIC_API_URL` in `.env.local` (defaults to `http://localhost:5002` if unset, see `frontend/src/lib/api.ts`).

## Architecture

### API (`PortfolioApi/`)
Minimal API style, no controllers. `Program.cs` is organized in three commented phases:
1. **Services** — DI registrations (OpenAPI, CORS policy named `"Frontend"` sourced from `AllowedOrigins` config, a `FirestoreDb` singleton and `FirestoreProjectStore`)
2. **Seed** — runs automatically on every startup, not just in Development; wrapped in try/catch that logs and continues
3. **Pipeline + Endpoints** — Scalar docs mounted at `/scalar` only in Development; endpoints registered via extension methods

Endpoints are grouped in `Endpoints/` as static classes with `Map*Endpoints(this WebApplication app)` extension methods (`ProjectsEndpoints`, `ContactEndpoints`, `HealthEndpoints`), each mapping its own route group. `GET /health` is the odd one out: it returns `{"status":"ok"}` without touching Firestore and is excluded from OpenAPI — it exists purely as the keep-alive ping target, so the ping costs no Firestore read quota. `Data/FirestoreProjectStore.cs` is the only place that talks to Firestore, injected into endpoints the way `AppDbContext` used to be. `Services/DataSeeder.cs` seeds two example `Project` documents idempotently.

Firestore document ids are **strings** (20 chars), so `Project.Id` is a `string` and the route is `/{id}`, not `/{id:int}`. `Project.CreatedAt` normalizes any `DateTimeKind` to UTC in its setter — Firestore throws `ArgumentException` on non-UTC `DateTime`. `Services/EmailService.cs` sends through the Resend HTTP API; `POST /api/contact` validates, sends, and is rate-limited to 3 requests/min. When the `Email` section is not configured (`IsConfigured` false) it degrades silently — it logs and reports success without sending.

`Models/` are plain classes with no separate DTOs — API responses serialize them directly (see `Project`, `ContactMessage`). `Project.Decisions` (JSON `decisions`) is an ordered list of `Decision` (`title`/`why`/`cost`) that feeds the project detail page; the order is meaningful, it is the sequence the reader should follow. It defaults to an **empty array, never null**: Firestore is schemaless, so documents written before the field existed simply lack the key, and consumers `.map` over it without a null check. `Project` carries Firestore mapping attributes (`[FirestoreData]`, `[FirestoreProperty]`, `[FirestoreDocumentId]`); `ContactMessage` is never persisted.

### Frontend (`frontend/`)
Next.js App Router, two routes. `src/app/page.tsx` composes section components in fixed order (`Navbar`, `Hero`, `About`, `Projects`, `Contact`), anchored via `id` for in-page nav — it is a **showcase only** and deliberately renders no decision or cost. `src/app/projects/[id]/page.tsx` is the detail page: it holds the reasoning, one `DecisionRecord` per decision, and is where the trade-off ledger lives. `generateStaticParams` pre-renders the known projects at build; because `getProjects` swallows failures and returns `[]`, an API that is down yields no params instead of a failed build, and those ids fall back to on-demand rendering. An unknown id calls `notFound()`.

Layout width is owned by two classes in `globals.css`, not by per-component `max-w-*`: `.shell` is the page container (96rem cap, padding that grows with the viewport) and `.measure` caps prose at 65ch. They are separate on purpose — a wide screen should widen the container, never the reading line. `design-system.test.ts` fails if `max-w-5xl` reappears.

`src/lib/api.ts` is the sole data-fetching boundary — typed `fetch` helpers against the API, using Next's `next: { revalidate: 60 }` for ISR. `Projects` (`src/components/Projects.tsx`) is an async server component that calls `getProjects()` directly; on fetch failure `getProjects` swallows the error and returns `[]`, and the component renders an empty-state message rather than an error. Because Render's free tier sleeps, `getProjects` retries network/timeout failures 3 times (15s each, 3s and 6s backoff, ~54s total) before giving up — HTTP 4xx/5xx is not retried. Budget for that when a call runs with the API down.

Styling is Tailwind v4 over a token system declared in `src/app/globals.css` — dark olive-graphite ground (`--ground`), signal amber accent (`--amber`), and `--clay` reserved for trade-off cost lines and errors. Use the generated utilities (`bg-ground`, `text-amber`, `border-rule`); a literal hex in a component fails `src/__tests__/design-system.test.ts`. Type roles are fixed: `font-display` (Bricolage Grotesque) for headlines and titles, `font-body` (Newsreader) for prose, `font-mono` (JetBrains Mono) for ids, labels and tags.

`next/font` variables live on `<html>`, not `<body>` — the theme tokens are declared on `:root`, and a custom property resolves where it is *declared*. Putting the font variables on `<body>` makes `--font-body` reference a `--font-newsreader` that does not exist at `:root`, so it becomes invalid and prose silently falls back to the default sans, with build, lint and tests all still green.

### Cross-cutting
CORS is locked to a single allowed origin read from API config (`AllowedOrigins`) — update both `appsettings.json`/environment config and the frontend's `NEXT_PUBLIC_API_URL` together when changing where either side is hosted.

Firestore credentials come from `Firestore:CredentialsJson` (the whole service account JSON, as
`Firestore__CredentialsJson` env var on Render — no credential file on disk). When empty, the app
falls back to Application Default Credentials. `EmulatorDetection.EmulatorOrProduction` is set, so
`FIRESTORE_EMULATOR_HOST` routes to a local emulator when present.

`.github/workflows/keep-api-awake.yml` pings `/health` every 10 minutes between 08:00 and 00:00 BRT to keep the Render instance awake, and needs a `RENDER_API_URL` repository variable. The window is deliberately not 24/7: Render grants 750 instance-hours/month and exceeding it suspends every free service until the next month. See `.specs/codebase/INTEGRATIONS.md`.

Project state, decisions and specs for this repo live in `.specs/` (see `.specs/project/STATE.md`).
