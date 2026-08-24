"use client";

import type { Decision } from "@/lib/api";

const fieldClass =
  "w-full rounded-sm border border-rule bg-ground px-3 py-2 font-body text-[0.9375rem] text-ink placeholder:text-ink-dim/60 transition-colors focus:border-amber/50";

const iconButtonClass =
  "label rounded-sm border border-rule px-2 py-1 transition-colors hover:border-amber/50 hover:text-ink disabled:cursor-not-allowed disabled:opacity-30 disabled:hover:border-rule disabled:hover:text-ink-dim";

interface Props {
  decisions: Decision[];
  onChange: (decisions: Decision[]) => void;
}

/**
 * A ordem da lista é conteúdo, não detalhe de armazenamento: é a sequência em que o leitor deve
 * percorrer as decisões na página do projeto (AD-008). Por isso o editor tem mover para cima e
 * para baixo, e por isso o número exibido acompanha a posição.
 */
export default function DecisionEditor({ decisions, onChange }: Props) {
  const alterar = (indice: number, campo: keyof Decision, valor: string) =>
    onChange(decisions.map((d, i) => (i === indice ? { ...d, [campo]: valor } : d)));

  const remover = (indice: number) => onChange(decisions.filter((_, i) => i !== indice));

  const mover = (indice: number, destino: number) => {
    const proxima = [...decisions];
    [proxima[indice], proxima[destino]] = [proxima[destino], proxima[indice]];
    onChange(proxima);
  };

  const adicionar = () => onChange([...decisions, { title: "", why: "", cost: "" }]);

  return (
    <section className="flex flex-col gap-4">
      <div className="flex items-baseline justify-between gap-4">
        <h3 className="label">Decisions</h3>
        <span className="label">{decisions.length}/10</span>
      </div>

      {decisions.length === 0 && (
        <p className="font-body text-[0.9375rem] text-ink-dim">
          No decisions yet. A project without a decision is a screenshot — add the first one.
        </p>
      )}

      <ol className="flex flex-col gap-6">
        {decisions.map((decision, indice) => (
          <li key={indice} className="border-l border-rule pl-4">
            <div className="mb-3 flex items-center justify-between gap-3">
              <span className="label text-amber">{String(indice + 1).padStart(2, "0")}</span>
              <div className="flex gap-2">
                <button
                  type="button"
                  className={iconButtonClass}
                  aria-label="Move decision up"
                  disabled={indice === 0}
                  onClick={() => mover(indice, indice - 1)}
                >
                  ↑
                </button>
                <button
                  type="button"
                  className={iconButtonClass}
                  aria-label="Move decision down"
                  disabled={indice === decisions.length - 1}
                  onClick={() => mover(indice, indice + 1)}
                >
                  ↓
                </button>
                <button
                  type="button"
                  className={iconButtonClass}
                  aria-label={`Remove decision ${indice + 1}`}
                  onClick={() => remover(indice)}
                >
                  Remove
                </button>
              </div>
            </div>

            <div className="flex flex-col gap-3">
              <div>
                <label className="label mb-1 block" htmlFor={`decision-title-${indice}`}>
                  What was decided
                </label>
                <input
                  id={`decision-title-${indice}`}
                  className={fieldClass}
                  value={decision.title}
                  maxLength={120}
                  onChange={(e) => alterar(indice, "title", e.target.value)}
                />
              </div>

              <div>
                <label className="label mb-1 block" htmlFor={`decision-why-${indice}`}>
                  Why
                </label>
                <textarea
                  id={`decision-why-${indice}`}
                  className={`${fieldClass} min-h-24 resize-y`}
                  value={decision.why}
                  maxLength={1500}
                  onChange={(e) => alterar(indice, "why", e.target.value)}
                />
              </div>

              <div>
                <label className="label mb-1 block" htmlFor={`decision-cost-${indice}`}>
                  What it cost <span className="normal-case">(optional)</span>
                </label>
                <textarea
                  id={`decision-cost-${indice}`}
                  className={`${fieldClass} min-h-20 resize-y`}
                  value={decision.cost ?? ""}
                  maxLength={1500}
                  onChange={(e) => alterar(indice, "cost", e.target.value)}
                />
              </div>
            </div>
          </li>
        ))}
      </ol>

      <div>
        <button
          type="button"
          className="label rounded-sm border border-rule px-3 py-2 transition-colors hover:border-amber/50 hover:text-ink disabled:cursor-not-allowed disabled:opacity-30"
          onClick={adicionar}
          disabled={decisions.length >= 10}
        >
          + Add decision
        </button>
      </div>
    </section>
  );
}
