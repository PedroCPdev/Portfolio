# Pedro Chasci Puga — Portfolio

Personal portfolio with a **Next.js** frontend and a **C# REST API** backed by PostgreSQL. The API serves project data that is rendered server-side, with a 60-second ISR cache.

**Live →** [pedrocpdev.vercel.app](https://pedrocpdev.vercel.app)

---

## Stack

| Layer | Tech |
|---|---|
| Frontend | Next.js 16, React 19, TypeScript, Tailwind CSS v4 |
| API | ASP.NET Core 9 (Minimal API), Entity Framework Core 9 |
| Database | PostgreSQL 16 |
| Docs | Scalar (OpenAPI) |
| Deployment | Vercel (frontend) |

---

## Project structure

```
Portfolio/
├── frontend/          # Next.js app
│   └── src/
│       ├── app/       # App Router layout, page, globals
│       ├── components/
│       └── lib/api.ts # Typed fetch helpers
└── PortfolioApi/      # ASP.NET Core Minimal API
    ├── Data/          # EF Core DbContext
    ├── Endpoints/     # Route groups (Projects, Contact)
    ├── Models/
    ├── Services/      # DataSeeder, EmailService
    └── Migrations/
```

---

## Getting started

### Prerequisites

- [.NET SDK 9](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org)
- [PostgreSQL 16](https://www.postgresql.org)

---

### API

1. **Clone the repo**

   ```bash
   git clone https://github.com/PedroCPdev/Portfolio.git
   cd Portfolio/PortfolioApi
   ```

2. **Create `appsettings.json`** (copy from the example below and fill in your values)

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=portfolio_db;Username=YOUR_USER;Password=YOUR_PASSWORD"
     },
     "AllowedOrigins": "http://localhost:3000"
   }
   ```

3. **Run** (migrations and seed data are applied automatically on startup)

   ```bash
   dotnet run
   ```

   API will be available at `http://localhost:5002`.  
   Interactive docs at `http://localhost:5002/scalar`.

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

## API endpoints

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/projects` | List all projects |
| `GET` | `/api/projects/{id}` | Get a project by ID |
| `POST` | `/api/contact` | Send a contact message |

---

## Author

**Pedro Chasci Puga** — [github.com/PedroCPdev](https://github.com/PedroCPdev) · [linkedin.com/in/pedrocpdev](https://www.linkedin.com/in/pedrocpdev/)
