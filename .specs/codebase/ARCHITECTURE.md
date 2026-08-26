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
GitHub Actions (keep-api-awake, a cada 10 min das 08:00 às 00:00 BRT) ──────┘

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
1. **Keep-alive** — GitHub Actions pinga `/health` a cada 10 min numa janela de 16h/dia.
   Mecanismo detalhado em `INTEGRATIONS.md`; razão da janela em AD-005.
2. **Retry no frontend** — `getProjects()` (`frontend/src/lib/api.ts:17-42`) tenta 3 vezes com
   timeout de 15s e backoff de 3s e 6s, cobrindo ~54s. Erro HTTP 4xx/5xx **não** é repetido
   (não melhora com insistência); só timeout e falha de rede. Esgotadas as tentativas, retorna
   `[]` em vez de lançar, para não quebrar o `next build`.
