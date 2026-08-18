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
- **Trade-off:** não resolve por si só o cold start do Render — tratado depois pela
  feature `render-cold-start` (keep-alive + retry), não pela troca de banco.
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

### B-002: Service account do Firebase dentro do repositório — RESOLVIDO
- **Descrição:** o arquivo `portfoliodb-9215c-firebase-adminsdk-fbsvc-d864c5e256.json`
  (projeto `portfoliodb-9215c`) foi colocado em `PortfolioApi/`. Chave privada real.
- **Auditoria (2026-08-18):** nunca foi commitada. `git rev-list --all` + `git grep` não
  encontram `BEGIN PRIVATE KEY` nem `private_key` em nenhum commit. **Sem vazamento;
  rotação desnecessária.**
- **Contorno atual:** `.gitignore:8-14` ignora o formato (`*firebase-adminsdk*.json` e afins),
  não apenas este arquivo — chave nova ou renomeada continua protegida.
- **Resolvido (2026-08-18):** chave movida para `~/.config/gcloud/` com permissão 600,
  fora da árvore do repositório. Ver L-006 para o risco descoberto no caminho.

### B-003: `appsettings.json` local defasado em duas frentes — RESOLVIDO
- **Descrição:** o arquivo local (gitignored) ainda tem `ConnectionStrings:DefaultConnection`
  do Postgres, **não tem seção `Firestore`**, e a seção `Email` está no formato SMTP antigo
  (`Host`, `Port`, `Username`, `Password`) enquanto `EmailService` lê `ApiKey`/`FromAddress`/
  `ToAddress` (Resend) desde o commit b1d321b.
- **Efeito:** localmente o Firestore não conecta (falta `ProjectId`) e `IsConfigured` é false,
  então o contato degrada silenciosamente sem enviar e-mail.
- **Resolvido (2026-08-18):** `Firestore:ProjectId` adicionado; `ConnectionStrings` morto
  removido; seção `Email` convertida para o formato Resend (`ApiKey`/`FromAddress`/`ToAddress`),
  com as 4 chaves SMTP mortas eliminadas — inclusive a senha de app do Gmail, que já não era
  lida por nenhum código. App verificada subindo com a config nova.
- **Confirmado em produção (2026-08-18):** o usuário recebeu o e-mail de teste disparado pelo
  formulário — logo `Email__ApiKey` está corretamente configurada no Render. `Email:ApiKey`
  local segue vazia por escolha; afeta só o envio em desenvolvimento.
- **Ação do usuário:** a senha de app do Gmail não existe mais em disco, mas segue válida na
  conta Google; o usuário assumiu a revogação em 2026-08-18 — passo fora do repositório,
  não verificável a partir daqui.

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

### L-004: `ConvertTo<T>()` já devolve null para documento inexistente
Verificado no emulador: `Exists=False` → `ConvertTo<Project>()` retorna `null` (tipo de
referência). A checagem explícita de `snapshot.Exists` em `FirestoreProjectStore` é portanto
redundante — mantida por explicitar a intenção. Consequência prática: no teste de mutação, a
remoção dessa checagem é **mutação equivalente**, não lacuna de cobertura.

### L-005: `EmulatorDetection` padrão é `None`
`new FirestoreDbBuilder()` ignora `FIRESTORE_EMULATOR_HOST` por padrão. Sem definir
`EmulatorDetection.EmulatorOrProduction`, rodar a API local contra o emulador falha silenciosamente
indo para produção. Verificado por reflection.

### L-006: o SDK Web do .NET assa qualquer `.json` do projeto dentro da imagem
`Microsoft.NET.Sdk.Web` trata `**/*.json` como Content e copia para a saída do `publish`.
Com a service account em `PortfolioApi/`, um `docker build` local a colocou em `/app/` dentro
da imagem — junto com o `appsettings.json` e a senha do Gmail. Verificado inspecionando a
imagem construída.

Produção nunca foi afetada: o Render constrói a partir do clone do git, onde ambos estão
ignorados, e lê a credencial de `Firestore__CredentialsJson`. O `.gitignore` sozinho **não**
protege builds locais, porque o Docker lê do disco, não do git — daí o `PortfolioApi/.dockerignore`.

## Ideias Adiadas

- **Introduzir DTOs na API.** Hoje os models são serializados direto (C-4), então mudar
  um campo quebra o contrato público sem aviso do compilador. (2026-08-18)
- **Endpoints de escrita / painel admin** para cadastrar projetos sem abrir o console
  do Firebase. (2026-08-18)
