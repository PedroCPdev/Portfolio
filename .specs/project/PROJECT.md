# PROJECT.md

## Visão
Site de portfólio pessoal de Pedro (pedrocpdev). Frontend Next.js renderiza os projetos
server-side; API ASP.NET Core Minimal expõe os dados e recebe mensagens de contato.

## Objetivos
- Vitrine pública de projetos, sempre disponível, custo zero de hospedagem
- A própria API em C# é peça de portfólio — demonstra ASP.NET Core 9 idiomático
- Formulário de contato entregando e-mail de verdade (Resend)

## Stack
- Linguagem(ns): TypeScript (frontend), C# / .NET 9 (API)
- Framework(s): Next.js 16 + React 19 + Tailwind v4; ASP.NET Core 9 Minimal API, Scalar (OpenAPI)
- Banco de dados: Cloud Firestore (NoSQL) — migrado de PostgreSQL/Supabase em 2026-08-18 (ver AD-001)
- Infraestrutura/deploy: frontend na Vercel (pedrocpdev.vercel.app); API em Docker no Render (free tier)

## Restrições
- **Tudo em free tier.** Qualquer solução escolhida não pode depender de plano pago.
- **Free tier do Render dorme** após ~15 min sem tráfego → cold start de ~50s (ver B-001).
- Sem monorepo: `frontend/` e `PortfolioApi/` têm toolchains independentes, sem package.json/solution raiz.
- CORS travado numa única origem lida de `AllowedOrigins`; muda junto com `NEXT_PUBLIC_API_URL`.

## Fora de escopo (nível projeto)
- Área administrativa / CRUD autenticado de projetos (escrita é manual no console do Firestore)
- Multi-usuário, autenticação de visitante, comentários
