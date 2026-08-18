# Spec: Migração da persistência de PostgreSQL/Supabase para Cloud Firestore

Escopo classificado como: **Grande** (multi-componente: API + frontend, dependência nova, API externa)

## Problema

O Supabase free tier **pausa o projeto por inatividade**. Com a pausa, `db.Database.MigrateAsync()`
na FASE 2 do `Program.cs` lança, a exceção é re-lançada (`throw`) e **a API inteira não sobe** no
Render. Um banco indisponível derrubava toda a aplicação, inclusive o endpoint de contato, que
nem usa banco.

## Requisitos

### P1 — MVP (fatia vertical, demonstrável ponta a ponta)

**[FS-01]**
QUANDO a API inicia com `Firestore:ProjectId` e `Firestore:CredentialsJson` configurados
ENTÃO o sistema DEVE construir um `FirestoreDb` autenticado por service account, sem exigir
arquivo de credencial em disco.

**[FS-02]**
QUANDO `GET /api/projects` é chamado
ENTÃO o sistema DEVE retornar 200 com a lista de projetos da coleção `projects`,
ordenada por `createdAt` decrescente (mais recente primeiro).

**[FS-03]**
QUANDO `GET /api/projects/{id}` é chamado com um id existente
ENTÃO o sistema DEVE retornar 200 com o projeto, e o campo `id` DEVE conter o
identificador do documento Firestore (string).

**[FS-04]**
QUANDO `GET /api/projects/{id}` é chamado com um id inexistente
ENTÃO o sistema DEVE retornar 404, sem lançar exceção.

**[FS-05]**
QUANDO um projeto é gravado com `CreatedAt` cujo `DateTimeKind` não é `Utc`
ENTÃO o sistema DEVE normalizar para UTC antes de gravar, nunca propagando a
`ArgumentException` do SDK.

**[FS-06]**
QUANDO a API inicia e a coleção `projects` está vazia
ENTÃO o sistema DEVE semear os dois projetos de exemplo; e QUANDO já houver
qualquer documento, DEVE não semear nada (idempotência).

**[FS-07]**
QUANDO o Firestore está indisponível ou mal configurado durante o seed no startup
ENTÃO a API DEVE registrar o erro e **continuar subindo**, mantendo `POST /api/contact`
funcional. (Correção direta da causa raiz — antes, `throw` derrubava tudo.)

**[FS-08]**
QUANDO o frontend recebe um projeto da API
ENTÃO o tipo `Project.id` DEVE ser `string` (não `number`), pois o id do documento
Firestore é uma string de 20 caracteres.

### P2 — além do MVP

**[FS-09]**
QUANDO o desenvolvedor roda a suíte com `FIRESTORE_EMULATOR_HOST` definido
ENTÃO os testes de integração DEVEM executar contra o emulador Firestore local.

## Fora de escopo

- Resolver o cold start do Render (C-1 / B-001) — problema distinto, não é causado pelo banco
- Migrar os dados existentes do Postgres para o Firestore (o seed recria os 2 exemplos)
- Endpoints de escrita (POST/PUT/DELETE de projetos) — escrita segue manual no console Firebase
- Autenticação/autorização de qualquer endpoint
- Firestore security rules (acesso é só server-side via service account)
- Alterar o fluxo de contato/Resend

## Matriz de rastreabilidade

| ID | Tarefa | Implementação | Teste | Evidência | Status |
|---|---|---|---|---|---|
| FS-01 | T2 | `PortfolioApi/Program.cs:32,49,53` | e2e (boot real) | `e2e-http-APROVADO.md` §FS-01 | **Verificado** |
| FS-02 | T3 | `Data/FirestoreProjectStore.cs:20,23`; `Endpoints/ProjectsEndpoints.cs:12` | `FirestoreProjectStoreTests.cs:25,39` | `gate-api` 12/12 + `e2e` §FS-02 · mutação M1 → Failed:1 | **Verificado** |
| FS-03 | T3 | `Data/FirestoreProjectStore.cs:30`; `Models/Project.cs:14-15` | `FirestoreProjectStoreTests.cs:47` | `gate-api` + `e2e` §FS-03 | **Verificado** |
| FS-04 | T3 | `Data/FirestoreProjectStore.cs:35`; `Endpoints/ProjectsEndpoints.cs:19` | `FirestoreProjectStoreTests.cs:68,77` | `gate-api` + `e2e` §FS-04 | **Verificado** |
| FS-05 | T1 | `Models/Project.cs:41,44` | `FirestoreProjectStoreTests.cs:85,99` | `gate-api` · mutação M2 → Failed:2 | **Verificado** |
| FS-06 | T4 | `Services/DataSeeder.cs:13` | `FirestoreProjectStoreTests.cs:113,123,134` | `gate-api` · mutação M4 → Failed:2 + `e2e` §FS-06 | **Verificado** |
| FS-07 | T2 | `PortfolioApi/Program.cs:79,86,88` (try/catch **sem** throw) | e2e com banco fora do ar | `e2e-http-APROVADO.md` §FS-07 | **Verificado** |
| FS-08 | T6 | `frontend/src/lib/api.ts:4` | `npm run build` (typecheck) + `npm run lint` | gate `full` | **Verificado** |
| FS-09 | T5 | `PortfolioApi.Tests/FirestoreFixture.cs:16,21,31` | a própria suíte (12 testes) | `gate-api-APROVADO.md` | **Verificado** |

Status possíveis: Não iniciado · Em andamento · Verificado · Não atendido
