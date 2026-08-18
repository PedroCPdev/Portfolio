# CONCERNS.md

## C-1: Cold start do Render mascarado como "sem projetos" — MITIGADO (2026-08-18)
`getProjects()` (`frontend/src/lib/api.ts`) engole qualquer erro e retorna `[]`, e
`Projects.tsx` renderiza "no projects yet" — um cold start do Render (~50s no free tier) era
indistinguível de "banco vazio" para o visitante. Trocar o banco não resolveu isso.
Mitigação em duas camadas (feature `render-cold-start`): keep-alive por GitHub Actions das
08:00 às 00:00 BRT e retry com backoff cobrindo ~54s. **Resíduo:** fora dessa janela o serviço
ainda dorme; o primeiro visitante da madrugada paga o religamento, agora absorvido pelo retry
em vez de ver a seção vazia. Eliminar o resíduo exigiria plano pago ou mover a leitura para o Next.

## C-2: Cobertura de testes recente e estreita — MÉDIO
Até 2026-08-18 nenhum dos dois sub-projetos tinha test runner. Hoje existem
`PortfolioApi.Tests` (xUnit, contra o emulador Firestore) e `vitest` no frontend, mas a
cobertura se concentra no que as duas features tocaram: acesso ao Firestore, seed e o retry
de `getProjects`. Componentes React e `EmailService` seguem sem teste.

## C-3: `DateTime` não-UTC quebra a escrita no Firestore — MÉDIO
Verificado empiricamente: `ArgumentException: Conversion from DateTime to Timestamp requires
the DateTime kind to be Utc`. Todo `DateTime` gravado precisa de `DateTimeKind.Utc`.

## C-4: Sem DTOs
Models são serializados direto. Mudar um campo do model muda o contrato público da API
sem nenhum aviso do compilador.
