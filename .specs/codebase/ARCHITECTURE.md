# ARCHITECTURE.md

## Fluxo de dados

```
Visitante → Vercel (Next.js server component)
              └─ lib/api.ts  fetch ISR 60s
                   → Render (ASP.NET Core Minimal API)
                        ├─ GET  /api/projects       → FirestoreProjectStore → Cloud Firestore
                        ├─ GET  /api/projects/{id}  → FirestoreProjectStore → Cloud Firestore
                        └─ POST /api/contact        → EmailService → Resend  (rate limit 3/min)
```

## Camadas da API
1. **Endpoints/** — validação de entrada e shape da resposta. Sem lógica de negócio.
2. **Data/FirestoreProjectStore** — única peça que fala Firestore. Injetada nos endpoints,
   ocupando o lugar que o `AppDbContext` ocupava (mesma convenção: uma peça de dados injetada).
3. **Services/** — EmailService (Resend), DataSeeder (seed idempotente no startup).

## Program.cs — 3 fases
1. **Services**: OpenAPI, ProblemDetails, CORS (`"Frontend"`), `FirestoreDb` singleton,
   `FirestoreProjectStore`, EmailOptions/EmailService, RateLimiter (`"contact"`, 3/min).
2. **Seed**: `DataSeeder.SeedAsync` — idempotente, só popula se a coleção estiver vazia.
   Falha de seed **não derruba a aplicação** (ver AD-003).
3. **Pipeline + Endpoints**: Scalar em `/scalar` só em Development; CORS; RateLimiter; endpoints.

## Decisão-chave: sem migrations
Firestore é schemaless. A FASE 2 antiga (`db.Database.MigrateAsync()`) deixou de existir —
com ela sai a causa raiz do crash de startup quando o banco estava pausado.
