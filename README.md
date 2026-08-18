# Pedro Chasci Puga — Portfolio

Personal portfolio with a **Next.js** frontend and a **C# REST API** backed by Cloud Firestore. The API serves project data that is rendered server-side, with a 60-second ISR cache.

**Live →** [pedrocpdev.vercel.app](https://pedrocpdev.vercel.app)

---

## Stack

| Layer | Tech |
|---|---|
| Frontend | Next.js 16, React 19, TypeScript, Tailwind CSS v4 |
| API | ASP.NET Core 9 (Minimal API) |
| Database | Cloud Firestore (NoSQL, schemaless — no migrations) |
| Email | Resend |
| Docs | Scalar (OpenAPI) |
| Deployment | Vercel (frontend) · Render, Docker (API) |

---

## Project structure

```
Portfolio/
├── frontend/               # Next.js app
│   └── src/
│       ├── app/            # App Router layout, page, globals
│       ├── components/
│       └── lib/api.ts      # Typed fetch helpers
├── PortfolioApi/           # ASP.NET Core Minimal API
│   ├── Data/               # FirestoreProjectStore
│   ├── Endpoints/          # Route groups (Projects, Contact, Health)
│   ├── Models/
│   └── Services/           # DataSeeder, EmailService
├── PortfolioApi.Tests/     # xUnit, runs against the Firestore emulator
└── .github/workflows/      # keep-api-awake (pings the API so it does not sleep)
```

---

## Getting started

### Prerequisites

- [.NET SDK 9](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org)
- A Firebase/Google Cloud project with Firestore enabled, plus a service account key
- JDK 21+ — only to run the Firestore emulator for the tests

---

### API

1. **Clone the repo**

   ```bash
   git clone https://github.com/PedroCPdev/Portfolio.git
   cd Portfolio/PortfolioApi
   ```

2. **Create `appsettings.json`** — copy `appsettings.Example.json` and fill in your values.
   The file is gitignored; **never commit the service account JSON.**

   ```json
   {
     "AllowedOrigins": "http://localhost:3000",
     "Firestore": {
       "ProjectId": "YOUR_FIREBASE_PROJECT_ID",
       "CredentialsJson": "PASTE_THE_SERVICE_ACCOUNT_JSON_ON_ONE_LINE",
       "ProjectsCollection": "projects"
     },
     "Email": {
       "ApiKey": "re_YOUR_RESEND_API_KEY",
       "FromAddress": "onboarding@resend.dev",
       "ToAddress": "YOUR_EMAIL@gmail.com"
     }
   }
   ```

   Leaving `CredentialsJson` empty falls back to Application Default Credentials.
   Leaving the `Email` section empty makes `POST /api/contact` accept the message and log it
   without sending mail.

3. **Run** (the seed runs on startup and only writes if the collection is empty)

   ```bash
   dotnet run
   ```

   API at `http://localhost:5002`. Interactive docs at `http://localhost:5002/scalar` (Development only).

---

### Frontend

1. ```bash
   cd ../frontend
   npm install
   ```

2. **Create `.env.local`**

   ```env
   NEXT_PUBLIC_API_URL=http://localhost:5002
   ```

3. ```bash
   npm run dev
   ```

   Opens at `http://localhost:3000`.

---

## Tests

Frontend:

```bash
cd frontend && npm test
```

API — the Firestore emulator is required, and `firebase-tools` needs JDK 21+:

```bash
export JAVA_HOME=/usr/lib/jvm/java-25-openjdk
export PATH="$JAVA_HOME/bin:$PATH"
npx -y firebase-tools@latest emulators:start --only firestore --project demo-portfolio

# in another shell
export FIRESTORE_EMULATOR_HOST=127.0.0.1:8080
dotnet test PortfolioApi.Tests
```

Integration tests fail loudly when the emulator is not running — they are never skipped.

---

## Deployment

| Where | What | Configuration |
|---|---|---|
| Vercel | `frontend/` | `NEXT_PUBLIC_API_URL` pointing at the Render URL |
| Render | `PortfolioApi/` via `Dockerfile` | `AllowedOrigins`, `Firestore__ProjectId`, `Firestore__CredentialsJson`, `Email__ApiKey`, `Email__FromAddress`, `Email__ToAddress` |
| GitHub Actions | `keep-api-awake.yml` | repository variable `RENDER_API_URL` |

Render's free tier sleeps after ~15 min idle and takes ~1 min to wake. Two layers absorb that:
the workflow pings `/health` every 10 minutes between 08:00 and 00:00 BRT, and `getProjects()`
retries for ~54s before falling back to an empty list. The window is not 24/7 on purpose —
Render grants 750 instance-hours/month, and going over suspends every free service until the
next month.

---

## API endpoints

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/projects` | List all projects, newest first |
| `GET` | `/api/projects/{id}` | Get a project by its Firestore document id |
| `POST` | `/api/contact` | Send a contact message (rate-limited, 3/min) |
| `GET` | `/health` | Liveness ping for the keep-alive; does not touch Firestore |

---

## Author

**Pedro Chasci Puga** — [github.com/PedroCPdev](https://github.com/PedroCPdev) · [linkedin.com/in/pedrocpdev](https://www.linkedin.com/in/pedrocpdev/)
