# STACK.md

## frontend/
- Next.js 16.2.9 (App Router), React 19.2.4, TypeScript 5, Tailwind CSS v4
- Gerenciador: npm. Testes: vitest 3.2 (`npm test`), desde 2026-08-18.
- Ícones: react-icons 5.6, lucide-react 1.18

## PortfolioApi/
- .NET 9 (`global.json` fixa SDK 9.0.0, `rollForward: latestMinor`). RootNamespace `Portfolio`.
- ASP.NET Core 9 Minimal API, `Nullable` e `ImplicitUsings` habilitados
- Scalar.AspNetCore 2.16.2 (docs OpenAPI, só em Development)
- **Persistência: Google.Cloud.Firestore 4.4.0** (desde 2026-08-18; antes EF Core 9 + Npgsql)
- Gerenciador: NuGet.

## Versões verificadas (não presumidas)
- `Google.Cloud.Firestore` 4.4.0 é a última estável no NuGet (consultado em 2026-08-18)
- SDKs .NET presentes na máquina: 9.0.119 e 10.0.110
- JDK 25 em `/usr/lib/jvm/java-25-openjdk` (necessário p/ emulador Firestore: exige 21+)
