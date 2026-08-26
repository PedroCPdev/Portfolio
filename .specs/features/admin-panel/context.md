# Contexto de decisões — Painel administrativo

Ambiguidades resolvidas com o usuário antes da spec. Só COMO fazer o que já foi pedido.

## Mecanismo de autenticação
- **Pergunta:** senha + JWT da própria API, Firebase Authentication, ou chave estática em header?
- **Decisão:** senha + JWT emitido pela própria API.
- **Razão:** o usuário é o único administrador — cadastro, perfis e recuperação de senha não têm
  para quem existir. Firebase Auth traria SDK no bundle do frontend, dependência nova na API e
  configuração no console (fora do repositório) para o mesmo resultado. Chave estática em header
  foi descartada por não expirar e ficar guardada no browser inteira. O caminho escolhido ainda
  usa `JwtBearer` nativo do ASP.NET Core, que é peça demonstrável de portfólio (objetivo de
  `PROJECT.md`).

## Onde vive o painel
- **Pergunta:** rota `/admin` no site publicado, ou só local rodando o Next na máquina?
- **Decisão:** rota `/admin` no próprio site.
- **Razão:** o usuário quer publicar projeto sem depender de ter o repositório clonado e o
  `npm run dev` rodando.
- **Custo aceito:** a rota existe publicamente na Vercel. Quem descobrir a URL vê a tela de login
  e nada mais — a autorização real está na API, não na renderização da página. Mitigado por
  `noindex, nofollow` (ADM-19) e por não haver link para ela em nenhuma página pública.

## Alcance do CRUD
- **Pergunta:** CRUD completo, criar+editar sem excluir, ou só criar e editar decisões?
- **Decisão:** CRUD completo, incluindo `DELETE` e editor de decisões com reordenação.
- **Razão:** o objetivo declarado é substituir o console do Firebase; um painel sem exclusão
  mantém o console no fluxo.
- **Custo aceito:** `DELETE` é o endpoint mais perigoso da superfície pública. Compensado pela
  confirmação por digitação do título no painel (ADM-21) e pela autenticação obrigatória (ADM-05).

## Sugestões fora de escopo levantadas durante a discussão
- **Revalidação sob demanda do ISR** (webhook `revalidatePath` disparado ao salvar) — registrado
  também em `STATE.md` → Ideias Adiadas.
