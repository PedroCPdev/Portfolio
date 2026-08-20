# Spec: project-tradeoff-field

Escopo classificado como: **Médio** (um campo novo atravessando model, seed, contrato e frontend).

Fecha a metade que faltava do trade-off ledger (AD-007): o `Ledger` já renderiza a linha de custo,
mas o campo nunca existiu no contrato — hoje ele é estendido localmente em `Projects.tsx`.

## Requisitos

### P1 — MVP

**[TR-01] Round-trip no Firestore**
QUANDO um `Project` com `Tradeoff` preenchido for gravado e lido de volta
ENTÃO o sistema DEVE devolver exatamente o mesmo texto.

**[TR-02] Compatibilidade com documentos antigos**
QUANDO um documento gravado **antes** do campo existir (sem a chave `tradeoff`) for lido
ENTÃO o sistema DEVE devolver `Tradeoff` nulo, sem lançar exceção.

**[TR-03] Nome no contrato público**
QUANDO um `Project` for serializado para JSON
ENTÃO o campo DEVE aparecer como `tradeoff`, camelCase, igual aos demais opcionais.

**[TR-04] Seed carrega o custo**
QUANDO o seed rodar numa coleção vazia
ENTÃO os projetos semeados DEVEM ter `Tradeoff` preenchido — o seed é a única fonte
de exemplo do ledger para quem sobe o projeto do zero.

**[TR-05] Frontend consome o tipo real**
QUANDO `Projects.tsx` ler `tradeoff`
ENTÃO o campo DEVE vir da interface `Project` de `src/lib/api.ts`,
e o alias local `ProjectRecord` NÃO DEVE mais existir.

## Fora de escopo

- **Preencher `tradeoff` nos documentos que já estão em produção.** É escrita em dado real,
  feita à mão no console do Firebase pelo usuário. O seed não alcança produção: só roda em
  coleção vazia (`DataSeeder:16`).
- Endpoints de escrita / painel admin (segue fora de escopo no nível projeto).
- Tornar `tradeoff` obrigatório. Ele é opcional por construção: o ledger degrada sem ele.

## Matriz de rastreabilidade

| ID | Implementação | Teste | Evidência | Status |
|---|---|---|---|---|
| TR-01 | `Models/Project.cs:30` | `FirestoreProjectStoreTests.cs:146,161` | 18/18 no emulador | Verificado |
| TR-02 | `Models/Project.cs:30` | `FirestoreProjectStoreTests.cs:176` | 18/18 no emulador | Verificado |
| TR-03 | `Models/Project.cs:30` | `ProjectContractTests.cs:18,30` | 18/18 + JSON real do `GET /api/projects` | Verificado |
| TR-04 | `Services/DataSeeder.cs:21,30` | `FirestoreProjectStoreTests.cs:196` | 18/18 no emulador | Verificado |
| TR-05 | `lib/api.ts:9`, `Projects.tsx:22` | `design-system.test.ts:158` | 22/22 no frontend | Verificado |

Status possíveis: Não iniciado · Em andamento · Verificado · Não atendido

**Verificação ponta a ponta (não só unitária).** Com o emulador no ar, a API real subiu contra ele
e o seed rodou; `GET /api/projects` devolveu `"tradeoff"` em camelCase nos dois documentos, e o
frontend renderizou as duas linhas de custo no ledger. Screenshot em anexo na conversa.
Nenhum dado de produção foi tocado: a coleção do e2e foi `projects_e2e_tradeoff` no emulador.

**O risco que o TR-02 cobre.** É o único de verdade nesta mudança. O Firestore é schemaless, então
os documentos que já estão em produção não têm a chave `tradeoff`. Se `ConvertTo<Project>()`
estourasse com a chave ausente, `GET /api/projects` quebraria em produção no primeiro deploy —
e nenhum teste que só grava e lê pelo próprio model pegaria isso, porque o model sempre grava a
chave. Por isso o teste escreve um documento **cru**, via dicionário, sem a chave.
