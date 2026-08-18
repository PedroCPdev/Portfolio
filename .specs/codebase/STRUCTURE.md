# STRUCTURE.md

Dois sub-projetos independentes, sem package.json/solution na raiz.

```
frontend/src/
  app/               layout.tsx, page.tsx (compõe as seções), globals.css
  components/        Navbar, Hero, About, Projects, ContactForm, Contact, SectionHeader
  lib/api.ts         ÚNICA fronteira de fetch para a API
  lib/__tests__/     testes vitest de api.ts
PortfolioApi/
  Program.cs         3 fases comentadas: Services / Seed / Pipeline+Endpoints
  Endpoints/         static classes com extension method Map*Endpoints(this WebApplication)
  Models/            entidades simples, serializadas direto na resposta (sem DTOs)
  Services/          DataSeeder, EmailService
  Data/              FirestoreProjectStore (acesso a dados)
  Dockerfile         imagem publicada no Render
  .dockerignore      impede credencial/appsettings de entrar na imagem (L-006)
PortfolioApi.Tests/  xUnit contra o emulador Firestore
.github/workflows/   keep-api-awake.yml (keep-alive da API)
.specs/              estado do harness
```
