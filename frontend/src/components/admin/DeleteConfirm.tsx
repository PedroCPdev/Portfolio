"use client";

import { useState } from "react";

interface Props {
  title: string;
  onConfirm: () => void;
  onCancel: () => void;
}

/**
 * Exclusão exige digitar o título exato. A confirmação por clique único protege contra o clique
 * errado, mas não contra o clique distraído — digitar o nome obriga a olhar para qual projeto
 * está prestes a sumir. Comparação sem normalizar de propósito: quem digitou "portfolio api"
 * não conferiu o que ia apagar.
 */
export default function DeleteConfirm({ title, onConfirm, onCancel }: Props) {
  const [digitado, setDigitado] = useState("");
  const confere = digitado === title;

  return (
    <div className="flex flex-col gap-4 rounded-sm border border-clay/50 bg-paper p-6">
      <h3 className="font-display text-lg text-ink">Delete this project?</h3>

      <p className="font-body text-[0.9375rem] text-ink-dim measure">
        This removes <span className="text-ink">{title}</span> from the site and from the
        database. The decisions recorded on it go with it, and there is no undo.
      </p>

      <div>
        <label className="label mb-2 block" htmlFor="confirm-title">
          Type the title to confirm
        </label>
        <input
          id="confirm-title"
          className="w-full rounded-sm border border-rule bg-ground px-4 py-3 font-mono text-[0.875rem] text-ink transition-colors focus:border-clay/60"
          value={digitado}
          onChange={(e) => setDigitado(e.target.value)}
          autoComplete="off"
        />
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <button
          type="button"
          disabled={!confere}
          onClick={onConfirm}
          className="label rounded-sm border border-clay/60 px-4 py-2 text-clay transition-colors hover:bg-clay/10 disabled:cursor-not-allowed disabled:border-rule disabled:text-ink-dim disabled:opacity-40 disabled:hover:bg-transparent"
        >
          Delete project
        </button>
        <button
          type="button"
          onClick={onCancel}
          className="label rounded-sm border border-rule px-4 py-2 transition-colors hover:border-amber/50 hover:text-ink"
        >
          Cancel
        </button>
      </div>
    </div>
  );
}
