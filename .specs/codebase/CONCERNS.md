# CONCERNS.md

## C-1: Cold start do Render mascarado como "sem projetos" — ALTO
`getProjects()` (`frontend/src/lib/api.ts`) tem timeout de 8s e **engole qualquer erro**
retornando `[]`. `Projects.tsx` então renderiza "no projects yet". Resultado: um cold start
do Render (~50s no free tier) é indistinguível de "banco vazio" para o visitante.
Trocar o banco **não resolve isso** — ver B-001.

## C-2: Ausência total de testes — ALTO (endereçado nesta migração)
Nenhum dos dois sub-projetos tinha test runner. A migração introduz `PortfolioApi.Tests`
(xUnit) rodando contra o emulador Firestore.

## C-3: `DateTime` não-UTC quebra a escrita no Firestore — MÉDIO
Verificado empiricamente: `ArgumentException: Conversion from DateTime to Timestamp requires
the DateTime kind to be Utc`. Todo `DateTime` gravado precisa de `DateTimeKind.Utc`.

## C-4: Sem DTOs
Models são serializados direto. Mudar um campo do model muda o contrato público da API
sem nenhum aviso do compilador.
