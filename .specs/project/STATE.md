# STATE.md

Memória persistente do projeto.

## Decisões (AD-NNN)

### AD-001: Migrar persistência de PostgreSQL/Supabase para Cloud Firestore
- **Decisão:** trocar EF Core + Npgsql por `Google.Cloud.Firestore` 4.4.0.
- **Razão:** o free tier do Supabase pausa por inatividade; com o banco pausado o
  `MigrateAsync()` no startup lançava e a API inteira não subia no Render. O free tier
  do Firestore (Spark) não pausa. Dados são poucos e simples — NoSQL serve bem.
- **Trade-off:** perde-se SQL/joins e migrations versionadas; ganha-se disponibilidade.
  Contrato público muda: `id` deixa de ser `int` e passa a ser `string`.
- **Impacto:** afeta API e frontend. Decidido em 2026-08-18 pelo usuário.

### AD-002: Manter a leitura do Firestore dentro da API .NET
- **Decisão:** o Next.js continua falando com a API; a API fala com o Firestore.
  Alternativa avaliada e recusada: Next ler o Firestore direto.
- **Razão:** escolha explícita do usuário — preserva a arquitetura atual e mantém a
  API em C# como peça demonstrável do portfólio.
- **Trade-off:** **não resolve o cold start do Render** (ver B-001), que continua
  podendo exibir "no projects yet" mesmo com o Firestore no ar.
- **Impacto:** `GET /api/projects` permanece no caminho crítico da página.

### AD-003: Falha de seed não derruba mais o startup
- **Decisão:** a FASE 2 do `Program.cs` loga e segue em vez de `throw`.
- **Razão:** era a causa raiz do incidente — indisponibilidade do banco derrubava
  inclusive `/api/contact`, que não usa banco.
- **Trade-off:** a API pode subir "saudável" servindo lista vazia de projetos.
- **Impacto:** degradação parcial em vez de queda total.

### AD-004: Credencial do Firestore via JSON em variável de ambiente
- **Decisão:** `Firestore__CredentialsJson` com o service account inteiro; fallback
  para Application Default Credentials.
- **Razão:** Render não oferece disco conveniente para arquivo de credencial.
- **Trade-off:** JSON grande numa env var; exige cuidado para nunca commitar.
- **Impacto:** `appsettings.json` segue gitignored.

### AD-005: Keep-alive em janela de 16h, não 24/7
- **Decisão:** pingar `/health` a cada 10 min apenas entre 08:00 e 00:00 (BRT).
- **Razão:** o Render dá 750 instance-hours/mês e serviço dormindo não consome. 24/7 daria
  744 h/mês num mês de 31 dias — margem de 6 h. Estourar a cota faz o Render **suspender
  todos os serviços free até o mês seguinte**, trocando falha intermitente por falha total.
- **Trade-off:** madrugada continua com cold start; absorvido pelo retry do frontend.
- **Impacto:** consumo ~496 h/mês, margem de 254 h. Janela editável numa linha do workflow.

### AD-006: `/health` dedicado em vez de pingar `/api/projects`
- **Decisão:** endpoint próprio que não toca no Firestore.
- **Razão:** o ping roda ~96×/dia; usar `/api/projects` gastaria cota de leitura do banco
  sem ganho, e faria o keep-alive falhar junto com o banco.
- **Trade-off:** um endpoint a mais na superfície pública (sem dados, fora do OpenAPI).
- **Impacto:** ping responde em ~9 ms mesmo com o Firestore fora do ar.

## Bloqueios (B-NNN)

### B-001: Cold start do Render mascarado como "sem projetos" — MITIGADO (2026-08-18)
- **Descrição:** free tier do Render dorme após ~15 min. O cold start (~50s) estoura o
  timeout de 8s de `getProjects()`, que engole o erro e retorna `[]` — o visitante vê
  "no projects yet" como se o portfólio estivesse vazio.
- **Contorno atual:** duas camadas, ver `features/render-cold-start` e AD-005 —
  (1) keep-alive por GitHub Actions numa janela de 16h/dia, (2) retry com backoff no
  `getProjects` cobrindo ~54s.
- **Residual:** fora da janela (00:00–08:00 BRT) o serviço ainda dorme; o primeiro
  visitante nesse horário paga o cold start, agora absorvido pelo retry em vez de ver
  a seção vazia. Eliminar o resíduo exigiria plano pago ou mover a leitura para o Next.

### B-002: Service account do Firebase dentro do repositório — MITIGADO
- **Descrição:** o arquivo `portfoliodb-9215c-firebase-adminsdk-fbsvc-d864c5e256.json`
  (projeto `portfoliodb-9215c`) foi colocado em `PortfolioApi/`. Chave privada real.
- **Auditoria (2026-08-18):** nunca foi commitada. `git rev-list --all` + `git grep` não
  encontram `BEGIN PRIVATE KEY` nem `private_key` em nenhum commit. **Sem vazamento;
  rotação desnecessária.**
- **Contorno atual:** `.gitignore:8-14` ignora o formato (`*firebase-adminsdk*.json` e afins),
  não apenas este arquivo — chave nova ou renomeada continua protegida.
- **Caminho de resolução:** ideal é a chave viver fora da árvore do repositório
  (ex.: `~/.config/gcloud/`) apontada por `GOOGLE_APPLICATION_CREDENTIALS`. Decisão do usuário.

### B-003: `appsettings.json` local defasado em duas frentes — ABERTO
- **Descrição:** o arquivo local (gitignored) ainda tem `ConnectionStrings:DefaultConnection`
  do Postgres, **não tem seção `Firestore`**, e a seção `Email` está no formato SMTP antigo
  (`Host`, `Port`, `Username`, `Password`) enquanto `EmailService` lê `ApiKey`/`FromAddress`/
  `ToAddress` (Resend) desde o commit b1d321b.
- **Efeito:** localmente o Firestore não conecta (falta `ProjectId`) e `IsConfigured` é false,
  então o contato degrada silenciosamente sem enviar e-mail.
- **Contorno atual:** nenhum. Não editado — contém secrets reais do usuário.
- **Caminho de resolução:** usuário adiciona `Firestore:ProjectId = portfoliodb-9215c` e
  troca a seção `Email` para o formato Resend. A senha de app do Gmail que sobrou ali não é
  mais lida por nenhum código — convém revogá-la.

## Lições (L-NNN)

### L-001: Duas camadas de deprecação na credencial do Google Cloud
`FirestoreDbBuilder.JsonCredentials` **e** `GoogleCredential.FromJson` estão ambos
`[Obsolete]` por risco de segurança. O caminho atual é
`CredentialFactory.FromJson<ServiceAccountCredential>(json)` +
`GoogleCredential.FromServiceAccountCredential(...)`. Nenhuma das duas deprecações era
adivinhável — só apareceram ao compilar de verdade contra o assembly.

### L-002: Firestore rejeita DateTime que não seja Kind=Utc
`ArgumentException: Conversion from DateTime to Timestamp requires the DateTime kind to
be Utc`. Normalizar no model evita a exceção em runtime. Verificado no emulador.

### L-003: Emulador Firestore exige JDK 21+
`firebase-tools` recusa Java < 21. A máquina tem JDK 17 como padrão e JDK 25 em
`/usr/lib/jvm/java-25-openjdk` — apontar `JAVA_HOME` para o 25 antes de subir o emulador.

## Ideias Adiadas

- **Migrar os dados reais do Postgres para o Firestore.** Nesta feature o seed apenas
  recria os 2 projetos de exemplo. Se houver projetos reais no Supabase, exportar antes
  que a instância seja descartada. (2026-08-18)
- **Introduzir DTOs na API.** Hoje os models são serializados direto (C-4), então mudar
  um campo quebra o contrato público sem aviso do compilador. (2026-08-18)
- **Endpoints de escrita / painel admin** para cadastrar projetos sem abrir o console
  do Firebase. (2026-08-18)

### L-004: `ConvertTo<T>()` já devolve null para documento inexistente
Verificado no emulador: `Exists=False` → `ConvertTo<Project>()` retorna `null` (tipo de
referência). A checagem explícita de `snapshot.Exists` em `FirestoreProjectStore` é portanto
redundante — mantida por explicitar a intenção. Consequência prática: no teste de mutação, a
remoção dessa checagem é **mutação equivalente**, não lacuna de cobertura.

### L-005: `EmulatorDetection` padrão é `None`
`new FirestoreDbBuilder()` ignora `FIRESTORE_EMULATOR_HOST` por padrão. Sem definir
`EmulatorDetection.EmulatorOrProduction`, rodar a API local contra o emulador falha silenciosamente
indo para produção. Verificado por reflection.

## Pendências

- **[Fora do escopo desta feature — reportado, não corrigido]** `CLAUDE.md` afirma que
  "`Services/EmailService.cs` is an unimplemented stub; `POST /api/contact` currently just
  validates and logs to console rather than sending mail". Isso está **desatualizado desde o
  commit b1d321b**, que implementou o envio via Resend. Imprecisão pré-existente, não
  introduzida pela migração Firestore. Corrigir numa tarefa própria.
- Usuário mencionou **dois problemas** mas descreveu apenas o primeiro (Supabase pausando).
  O segundo ainda não foi informado — perguntar antes de considerar o trabalho encerrado.
