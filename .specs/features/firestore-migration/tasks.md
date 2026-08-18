# Tasks — Migração para Cloud Firestore

### T1: modelo Project mapeado para Firestore

O quê:       `Project` com atributos Firestore, `Id` string, normalização UTC
Onde:        `PortfolioApi/Models/Project.cs`
Depende de:  nenhuma
Reutiliza:   nenhum (model novo)
Requisito:   FS-05

Concluída quando:
  [ ] Id é string com `[FirestoreDocumentId]`
  [ ] `CreatedAt` normaliza qualquer Kind para Utc no setter
  [ ] gate passa: `dotnet build`

Gate: build
Commit: refactor(api): mapear Project para documento do firestore

---

### T2: troca da dependência e da composição do Program.cs

O quê:       remover EF Core/Npgsql/migrations; registrar FirestoreDb + store; seed não derruba startup
Onde:        `PortfolioApi.csproj`, `Program.cs`, `Data/FirestoreOptions.cs`,
             deletar `Data/AppDbContext.cs` e `Migrations/`
Depende de:  T1
Reutiliza:   padrão de options de `Services/EmailService.cs:8`; try/catch de `Program.cs:52`
Requisito:   FS-01, FS-07

Concluída quando:
  [ ] `Google.Cloud.Firestore 4.4.0` referenciado; EF Core e Npgsql removidos
  [ ] credencial via `CredentialFactory` (0 warnings de obsoleto)
  [ ] seed em try/catch **sem** `throw`
  [ ] gate passa: `dotnet build`

Gate: build
Commit: feat(api): substituir ef core/postgres por cloud firestore

---

### T3: FirestoreProjectStore + endpoints

O quê:       store com List/GetById; endpoints usando a store; rota `{id}` sem `:int`
Onde:        `Data/FirestoreProjectStore.cs`, `Endpoints/ProjectsEndpoints.cs`
Depende de:  T2
Reutiliza:   `Map*Endpoints` de `Endpoints/ProjectsEndpoints.cs:9`
Requisito:   FS-02, FS-03, FS-04

Concluída quando:
  [ ] lista ordenada por `createdAt` desc
  [ ] id existente → 200 com `id` string preenchido
  [ ] id inexistente → 404 sem exceção
  [ ] gate passa: `dotnet build`

Gate: build
Commit: feat(api): ler projetos do firestore via store dedicada

---

### T4: seed idempotente no Firestore

O quê:       DataSeeder usando contagem da coleção
Onde:        `Services/DataSeeder.cs`
Depende de:  T3
Reutiliza:   guarda de idempotência de `Services/DataSeeder.cs:11`
Requisito:   FS-06

Concluída quando:
  [ ] coleção vazia → semeia 2 projetos
  [ ] coleção não vazia → não semeia (rodar 2x não duplica)
  [ ] gate passa: `dotnet build`

Gate: build
Commit: feat(api): semear projetos no firestore de forma idempotente

---

### T5: projeto de testes contra o emulador

O quê:       xUnit cobrindo FS-02..FS-06 com isolamento por coleção
Onde:        `PortfolioApi.Tests/`
Depende de:  T4
Reutiliza:   nenhum (primeiro projeto de teste do repo)
Requisito:   FS-09 (+ evidência de FS-02..FS-06)

Concluída quando:
  [ ] testes rodam contra emulador e passam de verdade
  [ ] nenhum teste com skip/ignore para forçar verde
  [ ] gate passa: `dotnet test`
  [ ] contagem de testes registrada em evidencias/

Tests: integration
Gate: quick
Commit: test(api): cobrir acesso ao firestore com emulador

---

### T6: frontend — id string

O quê:       `Project.id` de number para string
Onde:        `frontend/src/lib/api.ts`
Depende de:  nenhuma (paralelizável com T1-T5)
Reutiliza:   interface existente `frontend/src/lib/api.ts:3`
Requisito:   FS-08

Concluída quando:
  [ ] `id: string`
  [ ] gate passa: `npm run build` e `npm run lint`

Gate: full
Commit: refactor(frontend): tratar id de projeto como string

---

### T7: configuração e documentação

O quê:       appsettings.Example.json, CLAUDE.md, README com setup do Firestore
Onde:        `PortfolioApi/appsettings.Example.json`, `CLAUDE.md`, `README.md`
Depende de:  T6
Reutiliza:   estrutura do exemplo atual
Requisito:   suporte a FS-01

Concluída quando:
  [ ] exemplo mostra `Firestore:ProjectId` e `CredentialsJson` sem segredo real
  [ ] docs descrevem env vars do Render e emulador local

Gate: build
Commit: docs: documentar configuracao do firestore
