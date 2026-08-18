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

Endpoints are grouped in `Endpoints/` as static classes with `Map*Endpoints(this WebApplication app)` extension methods (`ProjectsEndpoints`, `ContactEndpoints`), each mapping its own route group. `Data/FirestoreProjectStore.cs` is the only place that talks to Firestore, injected into endpoints the way `AppDbContext` used to be. `Services/DataSeeder.cs` seeds two example `Project` documents idempotently.

Firestore document ids are **strings** (20 chars), so `Project.Id` is a `string` and the route is `/{id}`, not `/{id:int}`. `Project.CreatedAt` normalizes any `DateTimeKind` to UTC in its setter — Firestore throws `ArgumentException` on non-UTC `DateTime`. `Services/EmailService.cs` is an unimplemented stub; `POST /api/contact` currently just validates and logs to console rather than sending mail.

`Models/` are plain classes with no separate DTOs — API responses serialize them directly (see `Project`, `ContactMessage`). `Project` carries Firestore mapping attributes (`[FirestoreData]`, `[FirestoreProperty]`, `[FirestoreDocumentId]`); `ContactMessage` is never persisted.

### Frontend (`frontend/`)
Next.js App Router. `src/app/page.tsx` composes section components in fixed order (`Navbar`, `Hero`, `About`, `Projects`, `Contact`) inside a single-page layout; sections are anchored via `id` for in-page nav.

`src/lib/api.ts` is the sole data-fetching boundary — typed `fetch` helpers against the API, using Next's `next: { revalidate: 60 }` for ISR. `Projects` (`src/components/Projects.tsx`) is an async server component that calls `getProjects()` directly; on fetch failure `getProjects` swallows the error and returns `[]`, and the component renders an empty-state message rather than an error.

Styling is Tailwind v4 with inline utility classes using a consistent dark-navy palette (`#050d1a`, `#0d1b2e`, accent `#5ba0f5`) rather than themed/token-based classes — match existing hex/opacity patterns (e.g. `text-[#e8f0fe]/40`) when adding UI rather than introducing new colors.

### Cross-cutting
CORS is locked to a single allowed origin read from API config (`AllowedOrigins`) — update both `appsettings.json`/environment config and the frontend's `NEXT_PUBLIC_API_URL` together when changing where either side is hosted.

Firestore credentials come from `Firestore:CredentialsJson` (the whole service account JSON, as
`Firestore__CredentialsJson` env var on Render — no credential file on disk). When empty, the app
falls back to Application Default Credentials. `EmulatorDetection.EmulatorOrProduction` is set, so
`FIRESTORE_EMULATOR_HOST` routes to a local emulator when present.

Project state, decisions and specs for this repo live in `.specs/` (see `.specs/project/STATE.md`).
