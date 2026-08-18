# STRUCTURE.md

Dois sub-projetos independentes, sem package.json/solution na raiz.

```
frontend/src/
  app/          layout.tsx, page.tsx (compõe as seções), globals.css
  components/   Navbar, Hero, About, Projects, ContactForm, Contact, SectionHeader
  lib/api.ts    ÚNICA fronteira de fetch para a API
PortfolioApi/
  Program.cs    3 fases comentadas: Services / Seed / Pipeline+Endpoints
  Endpoints/    static classes com extension method Map*Endpoints(this WebApplication)
  Models/       entidades simples, serializadas direto na resposta (sem DTOs)
  Services/     DataSeeder, EmailService
  Data/         FirestoreProjectStore (acesso a dados)
.specs/         estado do harness
```
