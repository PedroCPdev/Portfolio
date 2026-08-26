# Evidência — CT-04 (413) do POST /api/contact contra Kestrel real

**Data:** 2026-08-26 · **Feature:** `contact-input-validation` (C-5) · **Requisito:** CT-04

Por que este arquivo existe: o `WebApplicationFactory` sobe a aplicação num **TestServer em
memória**, que não aplica `IHttpMaxRequestBodySizeFeature` (L-009). Um corpo de 100 KB chega
inteiro e quem responde é a validação (`400`), escondendo o mecanismo que o CT-04 pede. O teste
automatizado (`ContactEndpointTests.Endpoint_de_contato_declara_o_limite_de_corpo_de_32_kb`)
afirma que o endpoint **declara** o teto; esta evidência mostra o servidor **honrando** a
declaração.

Como foi produzido: `dotnet run --no-launch-profile --urls http://127.0.0.1:5112` com
`ASPNETCORE_ENVIRONMENT=Production`, `Email__ApiKey` e `Email__ToAddress` **vazios** (o
EmailService degrada em silêncio, então nenhum e-mail real saiu) e o Firestore apontado para
`demo-portfolio-kestrel-probe`, sem emulador no ar — o seed falha, cai no try/catch de
`Program.cs:113-123` e a API sobe assim mesmo. Nenhum dado de produção foi tocado.

```
$ curl http://127.0.0.1:5112/health
{"status":"ok"}

corpo grande = 102462 bytes (100 KB), teto declarado = 32 KB
POST corpo de 100 KB              : (413, '')
POST email malformado             : (400, '{"success":false,"error":"Email must be a valid email address."}')
POST valido                       : (200, '{"success":true,"message":"Message sent!"}')
```

Log da aplicação durante as três requisições — **uma única** entrada de handler:

```
[Contact] From: Visitante <v@exemplo.dev>
Email section not configured; contact message from v@exemplo.dev was logged but not emailed
```

**Leitura:** das três requisições, só a válida chegou ao handler. O corpo de 100 KB foi cortado
pelo Kestrel antes da desserialização (`413`, sem corpo de resposta) e o e-mail malformado foi
cortado pelo `ValidationFilter` antes do handler (`400`, com o shape `{ success, error }` que o
frontend lê). As três couberam nas 3 requisições/minuto do rate limit de `/api/contact`; nenhuma
recebeu `503`.
