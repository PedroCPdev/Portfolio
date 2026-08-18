# ARCHITECTURE.md

## Fluxo de dados

```
Visitante → Vercel (Next.js server component)
              └─ lib/api.ts  fetch ISR 60s, 3 tentativas (~54s) em falha de rede
                   → Render (ASP.NET Core Minimal API)
                        ├─ GET  /api/projects       → FirestoreProjectStore → Cloud Firestore
                        ├─ GET  /api/projects/{id}  → FirestoreProjectStore → Cloud Firestore
                        ├─ POST /api/contact        → EmailService → Resend  (rate limit 3/min)
                        └─ GET  /health             → responde sem tocar no Firestore
                                                      ↑
GitHub Actions (keep-api-awake, a cada 10 min das 08:00 às 00:00 BRT) ──────┘
```

## Camadas da API
1. **Endpoints/** — validação de entrada e shape da resposta. Sem lógica de negócio.
2. **Data/FirestoreProjectStore** — única peça que fala Firestore. Injetada nos endpoints,
   ocupando o lugar que o `AppDbContext` ocupava (mesma convenção: uma peça de dados injetada).
3. **Services/** — EmailService (Resend), DataSeeder (seed idempotente no startup).

`/health` (`Endpoints/HealthEndpoints.cs`) foge dessa divisão de propósito: não passa por store
nem por serviço, porque existe só para o keep-alive acordar a instância sem gastar cota de
leitura do Firestore (AD-006). Fica fora do OpenAPI via `ExcludeFromDescription()`.

## Program.cs — 3 fases
1. **Services**: OpenAPI, ProblemDetails, CORS (`"Frontend"`), `FirestoreDb` singleton,
   `FirestoreProjectStore`, EmailOptions/EmailService, RateLimiter (`"contact"`, 3/min).
2. **Seed**: `DataSeeder.SeedAsync` — idempotente, só popula se a coleção estiver vazia.
   Falha de seed **não derruba a aplicação** (ver AD-003).
3. **Pipeline + Endpoints**: Scalar em `/scalar` só em Development; CORS; RateLimiter; endpoints
   (`MapProjectsEndpoints`, `MapContactEndpoints`, `MapHealthEndpoints`).

## Decisão-chave: sem migrations
Firestore é schemaless. A FASE 2 antiga (`db.Database.MigrateAsync()`) deixou de existir —
com ela sai a causa raiz do crash de startup quando o banco estava pausado.

## Resiliência ao cold start
O free tier do Render desliga a instância após ~15 min sem tráfego e leva ~1 min para religar.
Duas camadas independentes tratam isso, nenhuma delas no caminho do banco:
1. **Keep-alive** — GitHub Actions pinga `/health` a cada 10 min numa janela de 16h/dia.
   Mecanismo detalhado em `INTEGRATIONS.md`; razão da janela em AD-005.
2. **Retry no frontend** — `getProjects()` (`frontend/src/lib/api.ts:17-42`) tenta 3 vezes com
   timeout de 15s e backoff de 3s e 6s, cobrindo ~54s. Erro HTTP 4xx/5xx **não** é repetido
   (não melhora com insistência); só timeout e falha de rede. Esgotadas as tentativas, retorna
   `[]` em vez de lançar, para não quebrar o `next build`.
