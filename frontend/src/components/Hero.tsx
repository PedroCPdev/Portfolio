// Hero é a tese e só. As decisões saíram daqui para as páginas de projeto (PD-03) — juntar
// as duas coisas na home era o que deixava a primeira tela longa e sem foco.
export default function Hero() {
  return (
    <section className="border-t border-rule">
      <div className="shell grid gap-x-10 gap-y-6 pt-16 pb-16 md:pt-24 md:pb-24 lg:grid-cols-[var(--spine)_1fr]">
        <div className="label flex flex-row gap-4 pt-3 lg:flex-col lg:gap-1.5 lg:text-right">
          <span className="text-ink">Backend</span>
          <span>C# · .NET</span>
          <span>Brasil</span>
        </div>

        <div className="min-w-0">
          {/* Nome e tese no mesmo h1: numa pagina de portfolio o nome e o cabecalho
              principal, e separa-lo em outro elemento o tiraria da estrutura de titulos. */}
          <h1 className="m-0 flex flex-col gap-5">
            <span className="font-display text-[clamp(1.125rem,1.5vw,1.4rem)] font-medium tracking-[0.01em] text-ink">
              Pedro Chasci Puga
            </span>
            <span className="max-w-[18ch] font-display text-[clamp(2.5rem,6vw,4.5rem)] font-extrabold leading-[0.98] tracking-[-0.035em] text-ink">
              I build the boring parts that have to stay up.
            </span>
          </h1>

          <p className="measure mt-7 text-[1.0625rem] leading-relaxed text-ink-dim">
            Backend engineer working in C# and .NET. The API serving this page is one of mine —
            it sleeps on a free tier, wakes up in about a minute, and I decided that was worth
            it. Open any project to read the decisions behind it.
          </p>

          <div className="mt-9 flex flex-wrap items-center gap-3">
            <a
              href="#work"
              className="rounded-sm border border-amber/45 bg-amber/10 px-6 py-3 font-mono text-[0.8125rem] text-amber no-underline transition-colors hover:bg-amber/20"
            >
              See the work
            </a>
            <a
              href="#contact"
              className="rounded-sm border border-rule px-6 py-3 font-mono text-[0.8125rem] text-ink-dim no-underline transition-colors hover:border-ink-dim hover:text-ink"
            >
              Get in touch
            </a>
          </div>
        </div>
      </div>
    </section>
  );
}
