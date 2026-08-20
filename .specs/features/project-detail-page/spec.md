# Spec: project-detail-page

Escopo classificado como: **Grande** (modelo de dados novo, rota nova, duas páginas reestruturadas).

Reorganiza a informação: a home volta a ser vitrine de projetos, e o raciocínio de engenharia
— que hoje ocupa a home inteira — migra para uma página por projeto. Decidido com o usuário
em 2026-08-20.

## Requisitos

### P1 — MVP

**[PD-01] Decisões como lista**
QUANDO um projeto tiver decisões registradas
ENTÃO cada uma DEVE ter título, o porquê e o custo, e o conjunto DEVE ser uma lista ordenada
no documento — substituindo o campo `tradeoff` único, que é absorvido por ela.

**[PD-02] Compatibilidade com documentos sem decisões**
QUANDO um documento não tiver a chave `decisions` (todos os de produção hoje)
ENTÃO a leitura DEVE devolver lista vazia, sem lançar.

**[PD-03] Home é vitrine**
QUANDO a home renderizar a seção de trabalho
ENTÃO ela DEVE mostrar um card por projeto com título, resumo, tags e chamada para o detalhe,
e NÃO DEVE renderizar decisão nem custo — nem na seção de projetos, nem no hero.

**[PD-04] Card leva ao detalhe**
QUANDO o visitante acionar um card
ENTÃO o sistema DEVE navegar para `/projects/{id}`, uma URL própria e compartilhável.

**[PD-05] Página de detalhe**
QUANDO `/projects/{id}` renderizar
ENTÃO ela DEVE mostrar o projeto e uma seção por decisão, cada uma com o porquê e o custo,
e DEVE oferecer caminho de volta para a home.

**[PD-06] Id inexistente**
QUANDO `/projects/{id}` receber um id que não existe
ENTÃO o sistema DEVE responder com a página 404 do Next, não com erro nem página vazia.

**[PD-07] Projeto sem decisões**
QUANDO um projeto não tiver decisões
ENTÃO a página de detalhe DEVE renderizar sem a seção de decisões e sem espaço morto,
dizendo o que existe em vez de mostrar bloco vazio.

**[PD-08] Largura e grid responsivos**
QUANDO a viewport crescer
ENTÃO o contêiner DEVE acompanhar até ~1500px e o grid de projetos DEVE ir de 1 para 2 e para
3 colunas; a prosa DEVE permanecer limitada a ~65 caracteres por linha em qualquer largura.

**[PD-09] Gate verde**
QUANDO o gate rodar
ENTÃO `dotnet test`, `npm test`, `npm run build` e `npm run lint` DEVEM passar.

## Fora de escopo

- Preencher `decisions` nos documentos de produção — escrita em dado real, é do usuário.
- Endpoint de escrita / painel admin.
- Paginação, busca ou filtro de projetos.
- Imagem de projeto (`imageUrl` continua no model, sem uso na UI).

## Matriz de rastreabilidade

| ID | Implementação | Teste | Evidência | Status |
|---|---|---|---|---|
| PD-01 | `Models/Decision.cs:11`, `Project.cs:31`, `lib/api.ts:4` | `FirestoreProjectStoreTests` round-trip + `ProjectContractTests` + `design-system.test.ts` | 20/20 API, 30/30 front | Verificado |
| PD-02 | `Models/Project.cs:31` | `FirestoreProjectStoreTests` (documento cru sem a chave) | 20/20 no emulador | Verificado |
| PD-03 | `components/Projects.tsx`, `Hero.tsx` | `design-system.test.ts` (PD-03) | 30/30 + screenshot | Verificado |
| PD-04 | `components/Projects.tsx:13` | `design-system.test.ts` (PD-04) | `GET /projects/{id}` → 200 | Verificado |
| PD-05 | `app/projects/[id]/page.tsx`, `DecisionRecord.tsx` | `design-system.test.ts` (PD-05) | screenshot 1920/390 | Verificado |
| PD-06 | `app/projects/[id]/page.tsx:34` | `design-system.test.ts` (PD-04) | id inexistente → **404 real** | Verificado |
| PD-07 | `app/projects/[id]/page.tsx` | `design-system.test.ts` (PD-04) | 30/30 | Verificado |
| PD-08 | `globals.css:61,82`, `Projects.tsx:67`, `DecisionRecord.tsx:25` | `design-system.test.ts` (PD-08) | screenshot 390 / 768 / 1920 | Verificado |
| PD-09 | — | gate | 20 API + 30 front, lint 0, build 7/7 rotas | Verificado |

Status possíveis: Não iniciado · Em andamento · Verificado · Não atendido

**Verificação de composição visual continua sendo por inspeção.** O gate prova invariante mecânica
(o grid tem as classes de breakpoint, `max-w-5xl` não voltou, a home não importa `DecisionRecord`).
Que o resultado *fique bom* em 390, 768 e 1920 foi verificado por screenshot, e está marcado como
tal — não como teste.

**Duas correções feitas olhando o pixel, não o gate:**
1. As réguas de `Why`/`Cost` esticavam pela coluna inteira enquanto o texto parava em 65ch,
   deixando régua pendurada sobre vazio. Resolvido colocando os dois lado a lado em `lg:` — que
   além de corrigir o alinhamento é o que a decisão é: duas faces.
2. O `DecisionRecord` tinha um terceiro nível de indentação (rail → número → título). O número
   passou a ficar na mesma linha do título.

**Um achado do build:** a primeira build pré-renderizou `/projects/aaa111` e outros ids do mock
usado num teste anterior — cache de fetch do Next em `.next/`, keyed por URL, e `localhost:5002`
tinha servido outra coisa antes. `rm -rf .next` resolveu. Vale saber ao trocar o que responde numa
porta durante desenvolvimento.
