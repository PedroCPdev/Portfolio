# INTEGRATIONS.md

| Integração | Onde | Config | Observação |
|---|---|---|---|
| Cloud Firestore | `Data/FirestoreProjectStore.cs`, `Program.cs` | `Firestore:ProjectId`, `Firestore:CredentialsJson` | Free tier Spark **não pausa** por inatividade (motivo da migração) |
| Resend (e-mail) | `Services/EmailService.cs` | seção `Email` (`ApiKey`, `FromAddress`, `ToAddress`) | POST `https://api.resend.com/emails`; degrada silenciosamente se não configurado |
| Vercel | hospeda `frontend/` | `NEXT_PUBLIC_API_URL` | pedrocpdev.vercel.app |
| Render | hospeda `PortfolioApi/` (Docker) | `AllowedOrigins`, `Firestore__*`, `Email__*` | **free tier dorme ~15min** → cold start ~50s (ver C-1) |
| GitHub Actions | `.github/workflows/keep-api-awake.yml` | variável de repositório `RENDER_API_URL` | keep-alive da API; ver abaixo |

## Autenticação no Firestore
Service account JSON inteiro numa env var `Firestore__CredentialsJson` (Render não precisa de arquivo em disco).
Ordem de resolução em `Program.cs`: `CredentialsJson` → senão Application Default Credentials.
Emulador local: env var `FIRESTORE_EMULATOR_HOST=127.0.0.1:8080`.

**Nunca commitar** o service account JSON. `PortfolioApi/appsettings.json` é gitignored.

## Keep-alive da API no Render

**Pré-requisito fora do repositório: a variável `RENDER_API_URL` precisa existir em
Settings → Secrets and variables → Actions → Variables, apontando para a URL do serviço no
Render.** Sem ela o job falha com erro explícito em vez de pingar em silêncio.

O workflow `.github/workflows/keep-api-awake.yml:22` roda por `schedule` (`*/10 0-2,11-23 * * *`
em UTC — a cada 10 minutos entre 08:00 e 00:00 BRT) e também aceita `workflow_dispatch` manual.
Cada execução faz um `curl` em `GET {RENDER_API_URL}/health` com `--max-time 90`, porque o
religamento leva ~1 min; resposta diferente de 200 vira `::warning::`, não falha o job — o
objetivo é acordar a instância, não monitorar. `/health` (`PortfolioApi/Endpoints/HealthEndpoints.cs`)
devolve `{"status":"ok"}` sem tocar no Firestore, então o ping não consome cota de leitura do
banco e continua funcionando mesmo com o Firestore fora do ar.

Para saber que está vivo: `gh run list --workflow keep-api-awake` (ou a aba Actions) — a cada
10 min dentro da janela deve haver uma execução verde, e o log traz a linha
`GET <url>/health -> 200`.

Para mexer: a janela mora **numa única linha**, o `- cron:` do workflow; cada hora/dia a mais
soma ~31 h/mês ao consumo. Para desligar, remova o bloco `schedule:` (o `workflow_dispatch`
pode ficar). O timeout e o backoff do lado do frontend ficam em `frontend/src/lib/api.ts:17-18`.

**O que o keep-alive não cobre:** entre 00:00 e 08:00 BRT o serviço dorme de propósito — a
janela de 16h existe para não estourar as 750 instance-hours/mês do free tier (AD-005), o que
suspenderia todos os serviços free até o mês seguinte. Nesse intervalo o primeiro acesso paga o
cold start, absorvido pelo retry de `getProjects`, não pelo ping. O workflow também não vigia
saúde: ele não avisa ninguém se a API responder erro.
