# Tasks — Painel administrativo

**Baseline medido em 2026-08-24, antes de tocar em qualquer arquivo:**
API 20 testes · frontend 39 testes · total **59**. Nenhuma tarefa pode fazer esse número cair.
(`codebase/TESTING.md` diz "50 testes: 20 na API, 30 no frontend" — defasado; corrigido na T13.)

**Gates:** `quick` = `cd PortfolioApi.Tests && dotnet test` (exige emulador, ver L-003) ·
`front` = `cd frontend && npm test` · `full` = `quick` + `front` + `npm run build` + `npm run lint`.

---

### T1: Senha de admin — hash PBKDF2, verificação e gerador de hash

O quê:       `AdminOptions` + `AdminPasswordHasher` (hash/verify) + guard `hash-password` no `Program.cs`
Onde:        `PortfolioApi/Auth/AdminOptions.cs`, `PortfolioApi/Auth/AdminPasswordHasher.cs`, `PortfolioApi/Program.cs`
Depende de:  nenhuma
Reutiliza:   padrão `FirestoreOptions` (`Data/FirestoreOptions.cs`) para a classe de options
Requisito:   ADM-02, ADM-04
Design:      D-2, D-3

Concluída quando:
  [ ] senha correta contra o hash gerado → `true` (sucesso)
  [ ] hash vazio, malformado ou com iterações não numéricas → `false`, sem lançar (sem sucesso)
  [ ] senha errada do mesmo tamanho → `false` (falha)
  [ ] `dotnet run -- hash-password teste` imprime `210000.<salt>.<hash>` e encerra sem subir servidor
  [ ] gate passa: `quick`
  [ ] contagem: 20 → 24 (4 testes novos)

Tests: unit    Gate: quick
Commit: feat(api): adicionar hash pbkdf2 da senha de admin

---

### T2: Login e emissão de JWT

O quê:       `AdminTokenService` + `POST /api/admin/login` + wiring do JwtBearer + política de rate limit
Onde:        `PortfolioApi/Auth/AdminTokenService.cs`, `PortfolioApi/Auth/LoginRateLimitPolicy.cs`, `PortfolioApi/Endpoints/AdminEndpoints.cs`, `PortfolioApi/Program.cs`, `PortfolioApi/PortfolioApi.csproj`, `PortfolioApi.Tests/PortfolioApi.Tests.csproj`, `PortfolioApi.Tests/AdminApiFactory.cs`, `PortfolioApi.Tests/AdminAuthTests.cs`
Depende de:  T1
Reutiliza:   `Map*Endpoints` + `MapGroup`/`WithTags` (`Endpoints/ProjectsEndpoints.cs:7-10`); fábrica com Firestore em porta morta (C-5 → design D-6)
Requisito:   ADM-01, ADM-02, ADM-03, ADM-04
Design:      D-1, D-4, D-10

Dependências novas: `Microsoft.AspNetCore.Authentication.JwtBearer` 9.0.16 (API),
`Microsoft.AspNetCore.Mvc.Testing` (testes). Exige `public partial class Program;` no fim do `Program.cs`.

Concluída quando:
  [ ] senha correta → `200` com `token` e `expiresAt`, e o token abre um endpoint protegido (sucesso)
  [ ] `Admin:PasswordHash` vazio → `401`, mesmo com a senha "certa" (sem sucesso / fail-closed)
  [ ] senha errada → `401` com `{ success:false, error }` e sem `token` no corpo (falha)
  [ ] 6ª chamada dentro de 1 min → `429` (falha)
  [ ] gate passa: `quick`
  [ ] contagem: 24 → 29 (5 testes novos)

Tests: integration (HTTP, sem emulador)    Gate: quick
Commit: feat(api): emitir jwt de admin no login com rate limit

---

### T3: Validação declarativa de entrada

O quê:       `ValidationFilter<T>` + `[ValidateItems]` + DataAnnotations em `Project` e `Decision`
Onde:        `PortfolioApi/Validation/ValidationFilter.cs`, `PortfolioApi/Validation/ValidateItemsAttribute.cs`, `PortfolioApi/Models/Project.cs`, `PortfolioApi/Models/Decision.cs`, `PortfolioApi.Tests/ProjectValidationTests.cs`
Depende de:  nenhuma
Reutiliza:   desenho D-1 de `features/contact-input-validation/design.md`; shape `{ success, error }` de `Endpoints/ContactEndpoints.cs:14`
Requisito:   ADM-10
Design:      D-5, D-6

Concluída quando:
  [ ] projeto com título, descrição e decisões dentro dos limites → válido (sucesso)
  [ ] projeto sem decisões e sem tags → válido, listas vazias não são erro (sem sucesso)
  [ ] decisão com `title` ou `why` vazio, tag acima de 30 chars, 11 decisões, url `javascript:` → inválido, com mensagem que nomeia o campo (falha)
  [ ] gate passa: `quick`
  [ ] contagem: 29 → 36 (7 testes novos)

Tests: unit    Gate: quick
Commit: feat(api): validar entrada de projeto por dataannotations

---

### T4: Escrita no `FirestoreProjectStore`

O quê:       `UpdateAsync` (ler-e-gravar, preserva `createdAt`), `DeleteAsync` (precondição) e guarda de id inválido
Onde:        `PortfolioApi/Data/FirestoreProjectStore.cs`, `PortfolioApi.Tests/FirestoreProjectStoreTests.cs`
Depende de:  nenhuma
Reutiliza:   `FirestoreFixture.NewStore` (`PortfolioApi.Tests/FirestoreFixture.cs:36-44`); `AddAsync` já existe (`FirestoreProjectStore.cs:44`)
Requisito:   ADM-07, ADM-08, ADM-09, ADM-11
Design:      D-7, D-8, D-9

Concluída quando:
  [ ] `UpdateAsync` em id existente troca os campos e mantém o `createdAt` original (sucesso)
  [ ] `UpdateAsync`/`DeleteAsync` em id inexistente → `false`, sem criar nem lançar (sem sucesso)
  [ ] `UpdateAsync`/`DeleteAsync` com id contendo `/` → `false`, sem `ArgumentException` (falha)
  [ ] `DeleteAsync` em id existente → `true` e `GetByIdAsync` seguinte devolve `null`
  [ ] ordem das decisões sobrevive ao round-trip de gravação e leitura
  [ ] gate passa: `quick`
  [ ] contagem: 36 → 43 (7 testes novos)

Tests: integration (emulador)    Gate: quick
Commit: feat(api): adicionar escrita e remocao no store do firestore

---

### T5: Endpoints de escrita protegidos

O quê:       `POST`, `PUT` e `DELETE` em `/api/projects`, autenticados, validados e com limite de corpo
Onde:        `PortfolioApi/Endpoints/ProjectsEndpoints.cs`, `PortfolioApi.Tests/AdminProjectsEndpointTests.cs`
Depende de:  T2, T3, T4
Reutiliza:   grupo `/api/projects` existente (`Endpoints/ProjectsEndpoints.cs:9`); `AdminApiFactory` da T2, agora apontada para o emulador com coleção GUID
Requisito:   ADM-05, ADM-06, ADM-07, ADM-08, ADM-09, ADM-10, ADM-12
Design:      D-11, e o descarte de `id`/`createdAt` da entrada (Contratos)

Concluída quando:
  [ ] `POST` autenticado válido → `201`, `Location`, e o `GET` seguinte devolve o projeto (sucesso)
  [ ] `PUT` autenticado em id existente → `200` com `createdAt` original preservado mesmo quando o corpo manda outro
  [ ] `PUT`/`DELETE` em id inexistente → `404` e nada é criado (sem sucesso)
  [ ] escrita sem token, com token expirado e com assinatura adulterada → `401` e o documento não muda (falha)
  [ ] corpo acima de 64 KB → `413`
  [ ] `GET /api/projects` sem `Authorization` continua `200` (ADM-12, regressão)
  [ ] gate passa: `quick`
  [ ] contagem: 43 → 53 (10 testes novos)

Tests: integration (emulador + HTTP)    Gate: quick
Commit: feat(api): expor escrita autenticada de projetos

---

### T6: Configuração de exemplo e documentação da API

O quê:       seção `Admin` no `appsettings.Example.json` e instruções de geração do hash
Onde:        `PortfolioApi/appsettings.Example.json`, `.specs/codebase/INTEGRATIONS.md`
Depende de:  T2
Reutiliza:   formato das seções `Firestore`/`Email` já presentes no exemplo
Requisito:   ADM-04
Design:      D-3, D-4

Concluída quando:
  [ ] o exemplo tem `Admin:PasswordHash` e `Admin:JwtSigningKey` vazios, com o nome das env vars do Render
  [ ] `INTEGRATIONS.md` descreve o mecanismo no presente (o que dispara, o que acontece, como sei que está vivo, onde desligo)
  [ ] gate passa: `build`

Tests: none (só configuração e documentação — `TESTING.md` não exige)    Gate: build
Commit: docs(api): documentar configuracao do admin

---

### T7: Cliente de escrita do painel [P]

O quê:       `src/lib/admin.ts` — sessão, login, listagem, criação, edição, remoção, resultado discriminado
Onde:        `frontend/src/lib/admin.ts`, `frontend/src/lib/__tests__/admin.test.ts`
Depende de:  nenhuma (fecha contra o contrato do `design.md`, não contra a API rodando)
Reutiliza:   `fetch` mockado e resposta fake tipada (`frontend/src/lib/__tests__/api.test.ts:11-13`); tipos `Project`/`Decision` de `src/lib/api.ts:4-23`
Requisito:   ADM-14, ADM-18, ADM-22
Design:      D-12, D-13, D-14

Concluída quando:
  [ ] login com sucesso guarda o token na sessão e devolve `{ ok: true }` (sucesso)
  [ ] `204` do delete vira `{ ok: true }` sem tentar parsear corpo vazio (sem sucesso)
  [ ] `401` limpa a sessão e devolve `{ ok:false, status:401 }`; `500` e falha de rede devolvem erro legível sem limpar sessão (falha)
  [ ] nenhuma chamada de escrita usa `next: { revalidate }` — todas `cache: "no-store"`
  [ ] gate passa: `front`
  [ ] contagem: 39 → 48 (9 testes novos)

Tests: unit    Gate: front
Commit: feat(frontend): adicionar cliente autenticado de escrita

---

### T8: Editor de decisões

O quê:       componente que adiciona, edita, remove e reordena decisões
Onde:        `frontend/src/components/admin/DecisionEditor.tsx`, `frontend/src/components/admin/__tests__/DecisionEditor.test.tsx`, `frontend/package.json`
Depende de:  nenhuma
Reutiliza:   tokens e utilities de `globals.css` (AD-007); tipografia `font-display`/`font-body`/`font-mono` (`CONVENTIONS.md`)
Requisito:   ADM-17
Design:      D-15

Dependências novas (dev): `jsdom`, `@testing-library/react`, `@testing-library/user-event`.
Nenhuma entra no bundle. Sem `vitest.config.ts` — jsdom por docblock (verificado, bloco D).

Concluída quando:
  [ ] adicionar cria decisão vazia no fim e editar campo reflete no estado (sucesso)
  [ ] lista vazia renderiza o convite a adicionar, sem quebrar (sem sucesso)
  [ ] remover tira a decisão certa; "subir" na primeira e "descer" na última ficam desabilitados (falha)
  [ ] mover troca a ordem e a ordem exibida é a que sai no `onChange`
  [ ] gate passa: `front`
  [ ] contagem: 48 → 54 (6 testes novos)

Tests: unit (jsdom)    Gate: front
Commit: feat(frontend): adicionar editor de decisoes do painel

---

### T9: Formulário de projeto

O quê:       campos do projeto + editor de decisões + estados de envio e erro
Onde:        `frontend/src/components/admin/ProjectForm.tsx`, `frontend/src/components/admin/__tests__/ProjectForm.test.tsx`
Depende de:  T7, T8
Reutiliza:   estrutura de formulário controlado com estado de envio/erro (`frontend/src/components/ContactForm.tsx`)
Requisito:   ADM-16, ADM-20, ADM-22
Design:      D-14

Concluída quando:
  [ ] preencher e enviar chama o cliente com o payload exato, decisões na ordem exibida (sucesso)
  [ ] aberto para editar, vem preenchido com os valores atuais, decisões inclusive (ADM-20)
  [ ] erro da API aparece na tela em `--clay` e o formulário mantém o que foi digitado (falha)
  [ ] botão desabilitado enquanto envia, sem envio duplo
  [ ] gate passa: `front`
  [ ] contagem: 54 → 60 (6 testes novos)

Tests: unit (jsdom)    Gate: front
Commit: feat(frontend): adicionar formulario de projeto do painel

---

### T10: Confirmação de exclusão

O quê:       diálogo que só habilita excluir quando o título é digitado igual
Onde:        `frontend/src/components/admin/DeleteConfirm.tsx`, `frontend/src/components/admin/__tests__/DeleteConfirm.test.tsx`
Depende de:  T7
Reutiliza:   tokens; `--clay` na ação destrutiva (reserva de `CONVENTIONS.md` respeitada: erro e custo)
Requisito:   ADM-21
Design:      —

Concluída quando:
  [ ] título exato habilita e confirmar chama o delete uma vez (sucesso)
  [ ] cancelar fecha sem chamar nada (sem sucesso)
  [ ] título parcial, com espaço sobrando ou caixa trocada mantém o botão desabilitado (falha)
  [ ] gate passa: `front`
  [ ] contagem: 60 → 64 (4 testes novos)

Tests: unit (jsdom)    Gate: front
Commit: feat(frontend): exigir confirmacao por titulo para excluir projeto

---

### T11: Rota `/admin`

O quê:       página que alterna entre login, lista e formulário, e trata sessão expirada
Onde:        `frontend/src/app/admin/page.tsx`, `frontend/src/app/admin/layout.tsx`, `frontend/src/components/admin/AdminPanel.tsx`, `frontend/src/components/admin/LoginForm.tsx`, `frontend/src/components/admin/__tests__/AdminPanel.test.tsx`
Depende de:  T7, T9, T10
Reutiliza:   `.shell`/`.measure` (AD-009); `metadata` do `layout.tsx:26-30`
Requisito:   ADM-13, ADM-14, ADM-15, ADM-18, ADM-19
Design:      D-12, D-13

Concluída quando:
  [ ] sem sessão, renderiza só o login — nenhuma lista, nenhum botão de criar (ADM-13, sucesso)
  [ ] com sessão e nenhum projeto, mostra estado vazio e o botão de criar (sem sucesso)
  [ ] `401` numa ação volta ao login com aviso de sessão expirada e apaga a sessão (ADM-18, falha)
  [ ] senha errada mostra erro e não guarda token (ADM-14)
  [ ] `layout.tsx` da rota declara `robots: { index: false, follow: false }` (ADM-19)
  [ ] gate passa: `front`
  [ ] contagem: 64 → 70 (6 testes novos)

Tests: unit (jsdom)    Gate: front
Commit: feat(frontend): adicionar rota /admin com sessao e listagem

---

### T12: Invariantes de design do painel

O quê:       guardas em `design-system.test.ts` para o que o painel não pode regredir
Onde:        `frontend/src/__tests__/design-system.test.ts`
Depende de:  T11
Reutiliza:   as próprias funções `sourceFiles`/`read` do arquivo (`design-system.test.ts:7-22`)
Requisito:   ADM-19
Design:      —

Concluída quando:
  [ ] falha se `/admin` aparecer como link em `Navbar.tsx` ou em qualquer componente público (sucesso)
  [ ] falha se o `layout.tsx` de `admin/` perder o `robots`/`index: false` (falha)
  [ ] falha se um hex literal aparecer em `components/admin/` (a varredura existente já cobre `src/`; confirmar que alcança a pasta nova)
  [ ] gate passa: `front`
  [ ] contagem: 70 → 73 (3 testes novos)

Tests: unit    Gate: front
Commit: test(frontend): guardar invariantes do painel admin

---

### T13: Fechamento — matriz, decisões e gate completo

O quê:       matriz de rastreabilidade preenchida com `arquivo:linha`, `AD-012..AD-014` em `STATE.md`, revogação da linha de escopo em `PROJECT.md`, `C-13` em `CONCERNS.md`, evidência do gate `full`
Onde:        `.specs/features/admin-panel/spec.md`, `.specs/project/STATE.md`, `.specs/project/PROJECT.md`, `.specs/project/ROADMAP.md`, `.specs/codebase/CONCERNS.md`, `.specs/codebase/TESTING.md`, `.specs/codebase/ARCHITECTURE.md`, `.specs/features/admin-panel/evidencias/`
Depende de:  T1..T12
Reutiliza:   formato de evidência de `features/firestore-migration/evidencias/`
Requisito:   todos
Design:      D-9 (origem do C-13)

Concluída quando:
  [ ] todo ADM-NN da matriz tem implementação `arquivo:linha` **e** teste, ou está marcado NÃO ATENDIDO com o motivo
  [ ] gate `full` roda e a saída real vai para `evidencias/`
  [ ] contagem final ≥ 73 e nenhuma exclusão silenciosa
  [ ] `TESTING.md` atualizado com a contagem verdadeira (hoje diz 50, são 59 antes desta feature)

Tests: none (fechamento)    Gate: full
Commit: docs(specs): registrar painel admin e decisoes AD-012..AD-014

---

## Paralelismo

`[P]` só na **T7**: ela não depende de tarefa da API e roda no gate `front`, que não compartilha
estado com o `quick`. As tarefas da API são sequenciais entre si (T2 depende do hasher da T1,
T5 depende de T2/T3/T4) e as de componente dependem do cliente da T7. Os testes de integração da
API compartilham o emulador — mesmo com coleção GUID, não marco `[P]` para não misturar dois
gates numa execução só.
