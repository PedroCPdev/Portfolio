import Ledger from "./Ledger";

// O hero é a tese, e a prova vem logo abaixo dela: o registro real da decisão que
// sustenta esta própria página (AD-001 em .specs/project/STATE.md).
export default function Hero() {
  return (
    <section className="border-t border-rule">
      <div className="mx-auto grid max-w-5xl gap-x-10 gap-y-6 px-6 pt-20 pb-16 md:px-10 md:pt-28 md:pb-24 lg:grid-cols-[var(--spine)_1fr]">
        <div className="label flex flex-row gap-4 pt-3 lg:flex-col lg:gap-1.5 lg:text-right">
          <span className="text-ink">Backend</span>
          <span>C# · .NET</span>
          <span>Brasil</span>
        </div>

        <div className="min-w-0">
          <h1 className="m-0 max-w-[20ch] font-display text-[clamp(2.5rem,7vw,4.25rem)] font-extrabold leading-[0.98] tracking-[-0.035em] text-ink">
            I build the boring parts that have to stay up.
          </h1>

          <p className="mt-7 max-w-[54ch] text-[1.0625rem] leading-relaxed text-ink-dim">
            Backend engineer working in C# and .NET. The API serving this page is one of
            mine — it sleeps on a free tier, wakes up in about a minute, and I decided that
            was worth it. Every project below is listed with what the decision cost.
          </p>

          <div className="mt-9 flex flex-wrap items-center gap-3">
            <a
              href="#work"
              className="rounded-sm border border-amber/45 bg-amber/10 px-6 py-3 font-mono text-[0.8125rem] text-amber no-underline transition-colors hover:bg-amber/20"
            >
              Read the decisions
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

      {/* Prova imediata da tese: um registro verdadeiro, não um exemplo inventado. */}
      <div className="border-t border-rule bg-paper/40">
        <div className="mx-auto grid max-w-5xl gap-x-10 gap-y-5 px-6 py-12 md:px-10 md:py-16 lg:grid-cols-[var(--spine)_1fr]">
          <div className="label pt-1 lg:text-right">
            <span className="text-amber">AD-001</span>
          </div>
          <div className="min-w-0">
            <h2 className="m-0 mb-6 font-display text-[1.375rem] font-semibold tracking-[-0.015em] text-ink">
              Firestore over PostgreSQL
            </h2>
            <Ledger
              kept="The managed Postgres I was on paused after a stretch of inactivity, and a paused database took the entire API down with it — including the endpoints that never touched a database. Firestore's free tier does not pause."
              cost="No SQL, no joins, no versioned migrations. Document ids stopped being integers, so the public contract changed and the frontend changed with it."
            />
          </div>
        </div>
      </div>
    </section>
  );
}
