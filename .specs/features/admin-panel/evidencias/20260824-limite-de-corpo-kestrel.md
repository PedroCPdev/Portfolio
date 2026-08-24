# Evidência — limite de corpo e escrita autenticada contra Kestrel real

**Data:** 2026-08-24 · **Tarefa:** T5 · **Requisitos:** ADM-05, ADM-06, ADM-10 (parte do 413)

Por que este arquivo existe: o `WebApplicationFactory` sobe a aplicação num **TestServer em
memória**, que não aplica `IHttpMaxRequestBodySizeFeature`. Um corpo de 100 KB chega inteiro e
quem responde é a validação (`400`), escondendo o mecanismo que a spec pede. Contra o servidor de
verdade o comportamento é outro — abaixo, a saída real.

Como foi produzido: `dotnet run --no-launch-profile --urls http://127.0.0.1:5111` com
`Admin__PasswordHash` gerado por `dotnet run -- hash-password`, `Admin__JwtSigningKey` de teste,
`ASPNETCORE_ENVIRONMENT=Production` e o Firestore apontado para o emulador numa coleção própria
(`projects_kestrel_probe`). Nenhum dado de produção foi tocado.

```
$ curl -s -o /dev/null -w '%{http_code}' -X POST /api/admin/login  (senha correta)
200
$ curl -s -o /dev/null -w '%{http_code}' -X POST /api/admin/login  (senha errada)
401
POST corpo de 100 KB, autenticado : (413, '')
POST corpo valido, autenticado    : (201, '{"id":"HTC5FIjMOkBMVHVoEmoq","title":"Kestrel","description":"d","decisions":[],"tags":["x"],"githubUrl":null,')
POST corpo invalido, autenticado  : (400, '{"success":false,"error":"Title is required."}')
POST sem token                    : (401, '')
DELETE id inexistente, autenticado: (404, '')
$ GET /api/projects (anonimo)
200
```

**Leitura:** o `413` é do Kestrel, antes de qualquer código do endpoint rodar — o corpo não chega
a ser desserializado. O teste automatizado equivalente
(`AdminProjectsEndpointTests.Endpoints_de_escrita_declaram_o_limite_de_corpo_de_64_kb`) afirma que
os dois endpoints de escrita declaram esse teto; esta evidência mostra o servidor honrando a
declaração.
