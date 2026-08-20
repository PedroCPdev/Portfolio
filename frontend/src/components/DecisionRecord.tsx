import { Decision } from "@/lib/api";

interface Props {
  decision: Decision;
  index: number;
}

// Elemento-assinatura, agora na página de detalhe. A numeração aqui é honesta: a ordem das
// decisões é informação — é a sequência em que devem ser lidas — ao contrário da lista de
// projetos na home, onde a ordem não diz nada ao leitor.
//
// Em tela larga o porquê e o custo ficam lado a lado, que é o que uma decisão é: duas faces.
// Isso também mantém cada coluna dentro da largura de leitura sem deixar régua pendurada
// sobre espaço vazio.
export default function DecisionRecord({ decision, index }: Props) {
  return (
    <article className="border-t border-rule py-9">
      <h3 className="m-0 mb-7 flex items-baseline gap-4 font-display text-[1.375rem] font-semibold tracking-[-0.015em] text-ink">
        <span className="shrink-0 font-mono text-[0.75rem] font-normal tracking-[var(--tracking-label)] text-amber">
          {String(index + 1).padStart(2, "0")}
        </span>
        <span className="min-w-0">{decision.title}</span>
      </h3>

      <dl className="m-0 grid gap-x-12 gap-y-6 lg:grid-cols-2">
        <div className="min-w-0">
          <dt className="label flex items-center gap-3 text-amber">
            <span>Why</span>
            <span className="h-px flex-1 bg-amber/25" />
          </dt>
          <dd className="m-0 mt-2.5 text-[0.9375rem] leading-relaxed text-ink/85">
            {decision.why}
          </dd>
        </div>

        {decision.cost && (
          <div className="min-w-0">
            <dt className="label flex items-center gap-3 text-clay">
              <span>Cost</span>
              <span className="h-px flex-1 bg-clay/30" />
            </dt>
            <dd className="m-0 mt-2.5 text-[0.9375rem] leading-relaxed text-ink-dim">
              {decision.cost}
            </dd>
          </div>
        )}
      </dl>
    </article>
  );
}
