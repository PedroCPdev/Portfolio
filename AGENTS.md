# Portfolio (pedrocpdev)

> Instruções para agentes de IA (Claude Code, Codex, Cursor...). Mantenha curto: tudo aqui é lido em todo turno.
> Detalhes de arquitetura em `.specs/codebase/ARCHITECTURE.md`; riscos em `.specs/codebase/CONCERNS.md`.

**Cliente:** o próprio Pedro (projeto pessoal, uso de IA liberado) · **Visão:** portfólio com Next.js na Vercel e API ASP.NET Core no Render, dados no Firestore. Detalhes em `.specs/project/PROJECT.md`.

## Stack

- `frontend/`: Next.js 16 + React 19 + TypeScript 5 + Tailwind v4
- `PortfolioApi/`: ASP.NET Core 9 Minimal API + Google.Cloud.Firestore 4.4.0 + Scalar
- Sem monorepo: cada sub-projeto tem toolchain própria, sem package.json ou solution na raiz.
- **Skills de stack:** nenhuma ainda

## Comandos

| Para | Comando |
| --- | --- |
| Instalar | `cd frontend && npm install` |
| Rodar frontend | `cd frontend && npm run dev` (http://localhost:3000) |
| Rodar API | `cd PortfolioApi && dotnet run` (http://localhost:5002, seed automático) |
| Gerar hash da senha admin | `cd PortfolioApi && dotnet run -- hash-password <senha>` |
| **Verificar (gate)** | `node scripts/verify.mjs quick \| full \| build` |

O `verify` é a única definição de "passou". Não rode testes de outro jeito para declarar algo pronto.

- `quick` (~30s): lint, `tsc`, vitest e os testes da API que não usam Firestore.
- `full` (~2,5 min): `quick` + testes `Category=Firestore`, com o emulador subido por `firebase emulators:exec`. Exige JDK 21+.
- `build` (~2 min a mais): `full` + `next build` + `dotnet build -c Release`.

Configuração local: `PortfolioApi/appsettings.json` (gitignored) sai de `appsettings.Example.json`. O frontend lê `NEXT_PUBLIC_API_URL` de `frontend/.env.local` (padrão `http://localhost:5002`).

## Convenções

- **Nomes:** componentes React em PascalCase (`DecisionRecord.tsx`), libs em camelCase (`lib/api.ts`), C# no padrão .NET.
- **Estrutura:** API em `Endpoints/` (static class com `Map*Endpoints`), `Data/` (só `FirestoreProjectStore` fala com o Firestore), `Services/`, `Auth/`, `Validation/`, `Models/` sem DTO. Frontend em `src/app` (rotas), `src/components`, `src/lib` (`api.ts` público, `admin.ts` do painel).
- **Erros:** `getProjects` engole falha e devolve `[]` depois de 3 tentativas (`frontend/src/lib/api.ts`). `lib/admin.ts` devolve resultado discriminado e mostra o erro. O contato responde `{ success, error }`.
- **Design:** cores só por token (`bg-ground`, `text-amber`); hex literal em componente e `--clay` fora dos 3 papéis reservados quebram `design-system.test.ts`. Regras completas em `.specs/codebase/CONVENTIONS.md`.
- **Commits:** Conventional Commits em português, um commit por tarefa (`feat(api): limitar contato por visitante`).

## Testes

**Frameworks:** API: xUnit em `PortfolioApi.Tests/` · frontend: vitest + Testing Library (jsdom ligado por arquivo com `// @vitest-environment jsdom`)
**Onde ficam:** `PortfolioApi.Tests/*Tests.cs` · `frontend/src/**/__tests__/*.test.ts(x)`
**Nome dos testes:** começam com o ID do requisito quando houver (`PD-05 registro de decisão`)

Classe de teste da API que usa `FirestoreFixture` leva `[Trait("Category", "Firestore")]`. Sem o trait ela entra no `quick` e falha sem emulador.

### Matriz de cobertura

| Camada | Tipo de teste exigido | Localização |
| --- | --- | --- |
| Endpoints da API | integration (`WebApplicationFactory`) | `PortfolioApi.Tests/*EndpointTests.cs` |
| `FirestoreProjectStore` | integration contra o emulador | `PortfolioApi.Tests/FirestoreProjectStoreTests.cs` |
| Models, validação, auth | unit | `PortfolioApi.Tests/` |
| `lib/` do frontend | unit (fetch mockado) | `frontend/src/lib/__tests__/` |
| Componentes React | unit com jsdom, `@/lib/admin` mockado | `frontend/src/**/__tests__/` |
| Tokens e regras de design | unit lendo o código-fonte | `frontend/src/__tests__/design-system.test.ts` |

### Paralelismo

| Tipo | Seguro em paralelo? | Por quê |
| --- | --- | --- |
| Integração Firestore | Sim | cada teste usa uma coleção com sufixo GUID |
| Frontend (vitest) | Sim | nenhum teste faz rede nem compartilha estado |

## Regras do projeto

- Requisitos, design e tarefas vivem em `.specs/`. Siga o fluxo da skill `pedro-harness:spec-driven`.
- Nunca leia nem escreva `.env`, `.env.local` nem `PortfolioApi/appsettings.json`; os nomes das variáveis estão em `frontend/.env.example` e `PortfolioApi/appsettings.Example.json`.
- Tudo em free tier: nenhuma solução pode depender de plano pago (Render, Vercel, Firestore, Resend).
- CORS aceita uma única origem (`AllowedOrigins`). Mudar o host de um lado exige mudar `NEXT_PUBLIC_API_URL` junto.
