"use client";

import { useState, FormEvent } from "react";

interface Props {
  onSubmit: (password: string) => Promise<void>;
  /** Mensagem vinda de fora: senha errada, sessão expirada, API fora do ar. */
  error?: string;
}

export default function LoginForm({ onSubmit, error }: Props) {
  const [password, setPassword] = useState("");
  const [enviando, setEnviando] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (enviando) return;

    setEnviando(true);
    await onSubmit(password);
    setEnviando(false);
    setPassword("");
  }

  return (
    <form onSubmit={handleSubmit} className="measure flex flex-col gap-5">
      <div>
        <label className="label mb-2 block" htmlFor="password">
          Password
        </label>
        <input
          id="password"
          type="password"
          autoComplete="current-password"
          className="w-full rounded-sm border border-rule bg-paper px-4 py-3 font-body text-[0.9375rem] text-ink transition-colors focus:border-amber/50"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
      </div>

      {error && (
        <p role="alert" className="font-body text-[0.9375rem] text-clay">
          {error}
        </p>
      )}

      <div>
        <button
          type="submit"
          disabled={enviando}
          className="label rounded-sm border border-amber/60 bg-amber/10 px-4 py-2 text-ink transition-colors hover:bg-amber/20 disabled:cursor-not-allowed disabled:opacity-40"
        >
          {enviando ? "Signing in…" : "Sign in"}
        </button>
      </div>
    </form>
  );
}
