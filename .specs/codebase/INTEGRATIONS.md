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

## Área administrativa (`/api/admin` + `/admin`)

**Pré-requisito fora do repositório: `Admin__PasswordHash` e `Admin__JwtSigningKey` precisam
existir no Render.** Sem as duas a área fica desligada — o login responde 401 e nenhuma escrita
passa, de propósito (ADM-04). O `Program.cs` avisa no startup com
`[Admin] Área administrativa DESLIGADA`.

O que dispara: uma pessoa abrindo `/admin` no site e digitando a senha. `POST /api/admin/login`
(`PortfolioApi/Endpoints/AdminEndpoints.cs`) confere a senha contra o hash PBKDF2 de
`Admin__PasswordHash` e devolve um JWT HS256 de 8 horas, assinado com `Admin__JwtSigningKey`
(mínimo 32 bytes). O painel guarda esse token em `sessionStorage` e o manda em `Authorization:
Bearer` nas escritas — `POST`, `PUT` e `DELETE` de `/api/projects`, todas com
`RequireAuthorization()`, validação de corpo e teto de 64 KB. A leitura (`GET`) continua anônima.
O login aceita 5 tentativas por minuto no endpoint inteiro e responde 429 acima disso.

O token é stateless de propósito: a instância do Render dorme e sobe zerada, e uma sessão em
memória obrigaria a novo login a cada religamento. O preço é não haver logout do lado do
servidor — um token vazado vale até expirar.

Para saber que está vivo: entrar em `/admin` e ver a lista de projetos carregar. No log da API,
`[Admin] Login concedido até ...` para sucesso e `[Admin] Tentativa de login com senha incorreta`
para falha; se aparecer `Área administrativa DESLIGADA` no startup, falta configuração.

Para mexer: trocar a senha é gerar outro hash com `dotnet run -- hash-password <senha>` e
substituir `Admin__PasswordHash` no Render. **Trocar `Admin__JwtSigningKey` invalida na hora todos
os tokens emitidos** — é o botão de revogação de emergência. A duração da sessão é
`Admin:TokenLifetimeHours` (8h). Para desligar a área inteira, esvazie as duas variáveis.

**O que a área administrativa não cobre:** salvar não atualiza o site na hora — a home usa ISR de
60s (`next: { revalidate: 60 }`), então a mudança aparece em até um minuto. Não há upload de
imagem (`imageUrl` é URL digitada), rascunho, histórico de versões, log de auditoria nem
recuperação de senha: a senha esquecida se resolve gerando outro hash. E o rate limit do login continua
sendo do endpoint, não por visitante — o C-6 mudou isso só no contato, então quem esgotar a janela
do login ainda deixa o admin um minuto sem entrar.

## Rate limit do formulário de contato

**Não depende de configuração no Render.** Os números vivem no código
(`PortfolioApi/RateLimiting/ContactRateLimiter.cs:26,34`).

O que dispara: toda requisição que chega na API — o limitador é registrado como `GlobalLimiter`
(`Program.cs:121`) e roda no pipeline antes dos endpoints. Ele mesmo decide se a requisição lhe
interessa: só `POST`/qualquer método em `/api/contact` entra numa janela; para `/health`,
`/api/projects` e o login ele devolve `NoLimiter` e a requisição passa direto.

A ordem, quando é o contato:

1. `UseForwardedHeaders` (`Program.cs:160`, primeiro middleware) troca o `RemoteIpAddress` — que
   atrás do proxy do Render seria o do proxy — pelo endereço mais à direita do `X-Forwarded-For`,
   que é o que o proxy anexou. `ForwardLimit = 1` é o que garante isso: com 2, o middleware
   andaria um salto e passaria a acreditar no valor que o cliente escreveu.
2. **Janela do visitante:** 3 requisições por minuto na partição daquele IP. Estourou → `429`, e
   o encadeamento para aqui — o teto do passo 3 **não** é consultado.
3. **Teto do endpoint:** 20 requisições aceitas por minuto somando todo mundo. É o que limita
   quem alcança a aplicação sem passar pelo proxy e pode escrever qualquer `X-Forwarded-For`.

Sem `X-Forwarded-For` (ou com valor malformado) todos caem numa partição só e o limite continua
valendo — degrada para o comportamento anterior ao C-6, nunca para "sem limite".

Para saber que está vivo: quatro envios seguidos do mesmo navegador em menos de um minuto — o
quarto tem de voltar `429`. No formulário do site isso aparece como a mensagem de erro de envio.

Para mexer: `PorVisitantePorMinuto` e `TetoDoEndpointPorMinuto` em
`RateLimiting/ContactRateLimiter.cs`. Para desligar, remover `options.GlobalLimiter` de
`Program.cs`. A janela é fixa (não deslizante): ela zera de uma vez, não gradualmente.

**O que este mecanismo não cobre:** spam bem-formado dentro da cota — três mensagens por minuto
por IP saturam a cota do Resend (100 e-mails/dia) sem nunca esbarrar no limite; isso é o C-10.
Também não emite `Retry-After`, então o cliente não sabe quanto falta para a janela virar. E o
login tem política própria, com partição única, que este mecanismo não alcança.
