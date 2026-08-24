# Cadeia de Verificação — Painel administrativo

**Data:** 2026-08-24
**Natureza:** saída real de protótipos descartáveis em
`scratchpad/probe/{app,fs,val}` e `scratchpad/rtl`. Nenhuma linha abaixo foi escrita de memória.
Nenhum arquivo do repositório foi tocado para produzi-la.

**Ambiente:** .NET 9 · `Microsoft.AspNetCore.Authentication.JwtBearer` 9.0.16 ·
`Google.Cloud.Firestore` 4.4.0 contra o emulador em `127.0.0.1:8080` (JDK 25, ver L-003) ·
vitest 3.2.7 · `@testing-library/react` 16 · React 19.2.4.

```
### Bloco A — JwtBearer / rate limit / body limit (scratchpad/probe/app, dotnet 9)
1) POST protegido sem token:            401
2) POST protegido com token lixo:       401
3) POST com token valido:               201  Location: /api/admin-ish/bec3bc6d923b4caca124047e4212eb45
4) POST com token expirado:             401
5) POST com assinatura adulterada:      401
6) DELETE sem token:                    401
6b) cabecalho do 401:                   WWW-Authenticate: Bearer
7) corpo de 100 KB (limite 64 KB):       413
8) 6x POST /login (PermitLimit=5/min):  200 200 200 429 429 429 
9) PBKDF2 hash + verificacao:           {"encoded":"uQgV2jO1qPjfavRDFt7HgQ==.iaXi/ZzBOtUX3hmYk2BAvRSPtXF+37BVxtFPhK6OFwk=","match":true}

### Bloco B — Firestore contra o emulador (scratchpad/probe/fs, Google.Cloud.Firestore 4.4.0)
F1 SetAsync com [FirestoreDocumentId] preenchido: OK
F1b Id lido = 'meu-id-fixo' | ordem decisions = d1,d2,d3
F2 DeleteAsync em inexistente: NÃO lançou (UpdateTime=Timestamp: 2026-08-24T17:50:54.935404Z)
F3 após SetAsync sem tags/decisions: chaves = createdAt,decisions,description,githubUrl,tags,title
F3b tags=0 decisions=0 createdAt=2021-05-05T00:00:00.0000000Z
F4 Precondition.MustExist → RpcException StatusCode=NotFound
F5 Document("a/b") LANÇOU: ArgumentException: Path must have an odd number of elements (Parameter 'path')
F6 AddAsync id gerado: 'GVlqs6OMUUBBEj3R0NCE' (len=20)

### Bloco C — DataAnnotations em coleção (scratchpad/probe/val)
V1 TryValidateObject recursivo? ok=True erros=[]
V3 payload válido: ok=True
V2 com [ValidateItems]: ok=False erros=[Decision title is required. Decision why is required. | Tags: item excede 30 caracteres.]

### Bloco D — vitest 3 + jsdom + @testing-library/react 16 com React 19 (scratchpad/rtl)
 ✓ t/node.test.ts (1 test) 4ms
 ✓ t/comp.test.tsx (1 test) 64ms
 Test Files  2 passed (2)
      Tests  2 passed (2)
-- mesmo teste com "jsx": "preserve" no tsconfig:
⎯⎯⎯⎯⎯⎯⎯ Failed Tests 1 ⎯⎯⎯⎯⎯⎯⎯
 Test Files  1 failed | 1 passed (2)
      Tests  1 failed | 1 passed (2)
```

## O que cada resultado decidiu no design

| Saída | Vira decisão |
|---|---|
| A1–A6b: `401` em token ausente, malformado, expirado e adulterado | ADM-05 é alcançável com `.RequireAuthorization()` — sem middleware próprio |
| A4 só funciona com `ClockSkew = TimeSpan.Zero` | sem isso um token expirado ainda seria aceito por até 5 min (padrão da lib) |
| A7: `413` | D-11, limite de 64 KB por endpoint |
| A8: `200 200 200 429 429 429` | D-10, `429` só na política de login; a `contact` segue em `503` |
| A9: `match=true` | D-2, PBKDF2 + `FixedTimeEquals` |
| F2: delete em inexistente não lança | D-8 — sem `Precondition.MustExist`, ADM-08 daria `204` para id inventado |
| F3: `SetAsync` sobrescreve tudo | D-7 — `PUT` precisa ler antes para preservar `createdAt` |
| F5: `ArgumentException` em id com `/` | D-9, e o achado `C-13` sobre o `GET` já existente |
| F6/F1b: ordem `d1,d2,d3` preservada | ADM-11 é propriedade do Firestore, não precisa de campo de ordem |
| V1: `ok=True` num payload inválido | D-6 — sem `[ValidateItems]`, ADM-10 valeria só no primeiro nível |
| D: jsdom por docblock | não é preciso criar `vitest.config.ts`; os testes node ficam como estão |
