import { ReactNode } from "react";

interface Props {
  id: string;
  rail: ReactNode;
  title: string;
  children: ReactNode;
}

// Spine assimétrico (DR-04): trilho de metadados em mono à esquerda, conteúdo à direita.
// Único dono do grid e do espaçamento de seção — nenhuma seção define padding próprio,
// que é como as margens param de brigar entre si.
export default function Section({ id, rail, title, children }: Props) {
  return (
    <section id={id} className="border-t border-rule">
      <div className="mx-auto grid max-w-5xl gap-x-10 gap-y-5 px-6 py-16 md:px-10 md:py-24 lg:grid-cols-[var(--spine)_1fr]">
        <div className="label pt-2 lg:text-right">{rail}</div>
        <div className="min-w-0">
          <h2 className="m-0 mb-8 font-display text-[2rem] font-semibold leading-[1.05] tracking-[-0.02em] text-ink">
            {title}
          </h2>
          {children}
        </div>
      </div>
    </section>
  );
}
