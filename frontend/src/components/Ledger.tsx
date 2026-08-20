// Elemento-assinatura: o par decisão/custo. A linha de custo é o que nenhum portfólio
// publica — e é o argumento inteiro. Sem `cost`, o bloco simplesmente não aparece (DR-05).
interface Props {
  kept: string;
  cost?: string;
}

export default function Ledger({ kept, cost }: Props) {
  return (
    <dl className="m-0 flex flex-col gap-5">
      <div>
        <dt className="label flex items-center gap-3 text-amber">
          <span>Kept</span>
          <span className="h-px flex-1 bg-amber/25" />
        </dt>
        <dd className="m-0 mt-2 text-[0.9375rem] leading-relaxed text-ink/85">{kept}</dd>
      </div>
      {cost && (
        <div>
          <dt className="label flex items-center gap-3 text-clay">
            <span>Cost</span>
            <span className="h-px flex-1 bg-clay/30" />
          </dt>
          <dd className="m-0 mt-2 text-[0.9375rem] leading-relaxed text-ink-dim">{cost}</dd>
        </div>
      )}
    </dl>
  );
}
