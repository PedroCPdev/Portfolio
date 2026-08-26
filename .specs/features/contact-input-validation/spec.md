# Spec: Validação de entrada no POST /api/contact

Escopo classificado como: **Médio** (5 arquivos, decisão de design real, testes novos)

Origem: `codebase/CONCERNS.md` → **C-5 (ALTO)**, segundo item do backlog da auditoria de
segurança de 2026-08-18. O anterior (CVEs do frontend) saiu em `602d02b`.

**O problema em uma frase:** os `[Required]` de `Models/ContactMessage.cs:7-12` são decorativos
— Minimal API do .NET 9 não executa DataAnnotations — então a única validação real é a checagem
de vazio em `Endpoints/ContactEndpoints.cs:12-14`, e o endpoint aceita nome e mensagem de
tamanho ilimitado, e-mail em qualquer formato, e corpo de até 30 MB (default do Kestrel).

## Requisitos

### P1 — MVP (fatia vertical, demonstrável ponta a ponta)

**[CT-01]**
QUANDO chega um `POST /api/contact` com `Name`, `Email` ou `Message` ausente, nulo ou só espaços
ENTÃO o sistema DEVE responder `400` com `{ success: false, error: <texto> }` e NÃO DEVE chamar o Resend

**[CT-02]**
QUANDO chega um `POST /api/contact` cujo `Email` não tem formato de endereço de e-mail
ENTÃO o sistema DEVE responder `400` com `{ success: false, error: <texto> }` e NÃO DEVE chamar o Resend

**[CT-03]**
QUANDO chega um `POST /api/contact` com `Name` > 80, `Email` > 254 ou `Message` > 4000 caracteres
ENTÃO o sistema DEVE responder `400` com `{ success: false, error: <texto> }` e NÃO DEVE chamar o Resend

**[CT-04]**
QUANDO chega um `POST /api/contact` cujo corpo excede 32 KB
ENTÃO o sistema DEVE responder `413` e NÃO DEVE desserializar nem processar o corpo

**[CT-05]**
QUANDO chega um `POST /api/contact` com os três campos válidos e dentro dos limites
ENTÃO o sistema DEVE responder `200` com `{ success: true, ... }`, preservando o comportamento atual

## Limites escolhidos e por quê

| Campo | Limite | Razão |
|---|---|---|
| `Name` | 80 | cabe qualquer nome real; é o campo que vai para o *subject* do e-mail |
| `Email` | 254 | máximo de um endereço de e-mail por RFC 5321 |
| `Message` | 4000 | mensagem de formulário de portfólio; ~2 páginas de texto |
| corpo | 32 KB | pior caso legítimo é ~16 KB (4000 chars × 4 bytes de UTF-8 + campos); 32 KB dá o dobro de folga e ainda é 1000× menor que os 30 MB de hoje |

## Fora de escopo

- **Espelhar os limites no formulário** (`maxLength` nos inputs de `ContactForm.tsx`). É UX, não
  segurança: a barreira que importa é a do servidor, e o frontend não é caminho confiável.
- **C-6 (rate limit global).** É o próximo item do backlog; esta feature não mexe na política de
  rate limiting — apenas convive com ela (ver `design.md`, decisão sobre uma fábrica por teste).
- **C-10 (anti-spam).** Validar formato não impede spam bem-formado; honeypot/captcha é item próprio.
- **Anti-spoofing do `reply_to`.** O visitante segue podendo declarar o e-mail de outra pessoa —
  é inerente a formulário de contato sem confirmação, e não muda com validação de formato.
- **DTO separado para a entrada** (C-4). Segue sem DTO, conforme `CONVENTIONS.md`.

## Matriz de rastreabilidade

Todos os `arquivo:linha` foram lidos do código no fechamento, não de memória. Gate `quick` em
2026-08-26: 86 testes na API (era 71), zero pulados.

| ID | Implementação | Teste | Evidência | Status |
|---|---|---|---|---|
| CT-01 | `Models/ContactMessage.cs:18-19`, `:24-25`, `:30-31` (`[Required]` + `[NotBlank]`), executados por `Endpoints/ContactEndpoints.cs:42` | `ContactEndpointTests.cs:61` (6 casos: ausente e só-espaços nos três campos) | ✓ | Verificado |
| CT-02 | `Models/ContactMessage.cs:27` (`[EmailAddress]`) | `ContactEndpointTests.cs:74` (3 casos, um deles injeção de cabeçalho) | ✓ | Verificado |
| CT-03 | `Models/ContactMessage.cs:20`, `:26`, `:32` (`[StringLength]` 80/254/4000) | `ContactEndpointTests.cs:82`, `:89`, `:98` (acima do limite) e `:106` (exatamente no limite, aceito) | ✓ | Verificado |
| CT-04 | `Endpoints/ContactEndpoints.cs:16`, `:43` (`RequestSizeLimitAttribute` de 32 KB) | `ContactEndpointTests.cs:124` (declaração) | ✓ + `evidencias/20260826-limite-de-corpo-contato-kestrel.md` (o `413` do Kestrel real) | Verificado — o `413` é do servidor, e o TestServer não aplica limite de corpo (L-009); o teste afirma a declaração, a evidência mostra o servidor honrando |
| CT-05 | `Endpoints/ContactEndpoints.cs:23-38` (handler inalterado) | `ContactEndpointTests.cs:139` (200, `success: true`, e **uma** chamada ao Resend) | ✓ | Verificado |

Status possíveis: Não iniciado · Em andamento · Verificado · Não atendido

**5 de 5 verificados.** O "NÃO DEVE chamar o Resend" de CT-01..CT-03 é observado por um espião de
`HttpMessageHandler` (design D-8), não presumido: com o e-mail desconfigurado o teste passaria
sem poder falhar. Confirmação de que a suíte morde: removendo `.AddEndpointFilter` de
`ContactEndpoints.cs:42`, **12 dos 15 testes ficam vermelhos**; com ele, 15 verdes.
