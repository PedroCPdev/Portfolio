# INTEGRATIONS.md

| Integração | Onde | Config | Observação |
|---|---|---|---|
| Cloud Firestore | `Data/FirestoreProjectStore.cs`, `Program.cs` | `Firestore:ProjectId`, `Firestore:CredentialsJson` | Free tier Spark **não pausa** por inatividade (motivo da migração) |
| Resend (e-mail) | `Services/EmailService.cs` | seção `Email` (`ApiKey`, `FromAddress`, `ToAddress`) | POST `https://api.resend.com/emails`; degrada silenciosamente se não configurado |
| Vercel | hospeda `frontend/` | `NEXT_PUBLIC_API_URL` | pedrocpdev.vercel.app |
| Render | hospeda `PortfolioApi/` (Docker) | `AllowedOrigins`, `Firestore__*`, `Email__*` | **free tier dorme ~15min** → cold start ~50s (B-001) |

## Autenticação no Firestore
Service account JSON inteiro numa env var `Firestore__CredentialsJson` (Render não precisa de arquivo em disco).
Ordem de resolução em `Program.cs`: `CredentialsJson` → senão Application Default Credentials.
Emulador local: env var `FIRESTORE_EMULATOR_HOST=127.0.0.1:8080`.

**Nunca commitar** o service account JSON. `PortfolioApi/appsettings.json` é gitignored.
