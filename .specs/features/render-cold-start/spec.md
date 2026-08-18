# Spec: Cold start do Render deixar de esvaziar a seção de projetos

Escopo classificado como: **Médio** (dois componentes, sem domínio novo)

Resolve B-001. Problema relatado pelo usuário como "o Render parando".

## Problema

O free tier do Render derruba o serviço após 15 min sem tráfego; religar leva ~1 min.
`getProjects()` tem timeout de 8s e **engole qualquer erro retornando `[]`**, então o
visitante vê "no projects yet" — indistinguível de portfólio vazio.

Pior: com ISR (`revalidate: 60`), uma revalidação que falha **substitui dados bons por lista
vazia** no cache. Um único cold start no momento errado apaga a vitrine até a próxima
revalidação bem-sucedida.

## Restrição dura (verificada)

Render dá **750 instance-hours/mês por workspace** e serviços dormindo não consomem horas.
Um mês de 31 dias tem 744h. Manter o serviço acordado 24/7 deixa margem de 6h — e estourar
a cota faz o Render **suspender todos os serviços free até o mês seguinte**. Portanto
keep-alive 24/7 é inaceitável: troca uma falha intermitente por uma falha total.

## Requisitos

### P1 — MVP

**[RC-01]**
QUANDO o keep-alive precisa acordar a API
ENTÃO deve existir um endpoint dedicado que responda 200 **sem tocar no Firestore**,
para não consumir cota de leitura do banco a cada ping.

**[RC-02]**
QUANDO `getProjects()` falha por timeout ou erro de rede
ENTÃO o sistema DEVE tentar novamente com backoff antes de desistir, cobrindo ao menos
50 segundos no total — a ordem de grandeza de um cold start do Render.

**[RC-03]**
QUANDO todas as tentativas de `getProjects()` falham
ENTÃO o sistema DEVE retornar lista vazia (preservando o comportamento atual da UI),
nunca lançar durante o build.

**[RC-04]**
QUANDO o keep-alive é agendado
ENTÃO a frequência DEVE manter o consumo mensal com folga dentro das 750 instance-hours,
e a janela DEVE ser um único ponto de edição no arquivo de workflow.

## Fora de escopo

- Mover a leitura de projetos para o Next.js (contraria AD-002, decisão do usuário)
- Plano pago do Render
- Cache persistente de última-boa-resposta (Redis/KV) — resolveria melhor, mas adiciona
  infraestrutura; registrado como Ideia Adiada
- Cold start no `POST /api/contact` — o usuário está ativamente esperando; mitigado
  parcialmente pelo keep-alive, sem mudança de código

## Matriz de rastreabilidade

| ID | Implementação | Teste | Evidência | Status |
|---|---|---|---|---|
| RC-01 | `PortfolioApi/Endpoints/HealthEndpoints.cs:10` | e2e com Firestore inacessível | `evidencias/` §RC-01 — 200 em 9ms | **Verificado** |
| RC-02 | `frontend/src/lib/api.ts:17-18,25` | `api.test.ts:39,52` | vitest 5/5 · RED prévio: 3 falhas | **Verificado** |
| RC-03 | `frontend/src/lib/api.ts:32,42` | `api.test.ts:66,86` | vitest 5/5 | **Verificado** |
| RC-04 | `.github/workflows/keep-api-awake.yml:21` | cálculo de cota (16h/dia) | 496 h/mês vs cota 750 — margem 254 h | **Verificado** |

Status possíveis: Não iniciado · Em andamento · Verificado · Não atendido
