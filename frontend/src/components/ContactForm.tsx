"use client";

import { useState, FormEvent } from "react";
import { sendContactMessage } from "@/lib/api";

const fieldClass =
  "w-full rounded-sm border border-rule bg-paper px-4 py-3 font-body text-[0.9375rem] text-ink placeholder:text-ink-dim/60 transition-colors focus:border-amber/50";

const labelClass = "label mb-2 block";

type Status = "idle" | "submitting" | "success" | "error";

export default function ContactForm() {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState("");
  const [website, setWebsite] = useState("");
  const [status, setStatus] = useState<Status>("idle");
  const [errorMessage, setErrorMessage] = useState("");

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setStatus("submitting");

    const result = await sendContactMessage({ name, email, message, website });

    if (result.success) {
      setStatus("success");
      setName("");
      setEmail("");
      setMessage("");
      setWebsite("");
    } else {
      setStatus("error");
      setErrorMessage(result.error ?? "The message did not go through. Try again.");
    }
  }

  return (
    <form onSubmit={handleSubmit} className="relative flex w-full flex-col gap-5">
      {/* Honeypot (C-10): fora da tela, do teclado e do leitor de tela. Robô que preenche todo
          campo preenche este, e a API descarta a mensagem respondendo como se tivesse enviado. */}
      <div aria-hidden="true" className="absolute -left-[9999px] h-px w-px overflow-hidden">
        <label htmlFor="website">Website</label>
        <input
          id="website"
          name="website"
          type="text"
          tabIndex={-1}
          autoComplete="off"
          value={website}
          onChange={(e) => setWebsite(e.target.value)}
        />
      </div>
      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
        <div>
          <label htmlFor="name" className={labelClass}>
            Name
          </label>
          <input
            id="name"
            type="text"
            required
            value={name}
            onChange={(e) => setName(e.target.value)}
            className={fieldClass}
          />
        </div>
        <div>
          <label htmlFor="email" className={labelClass}>
            Email
          </label>
          <input
            id="email"
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className={fieldClass}
          />
        </div>
      </div>

      <div>
        <label htmlFor="message" className={labelClass}>
          Message
        </label>
        <textarea
          id="message"
          required
          rows={5}
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          className={`${fieldClass} resize-none`}
        />
      </div>

      <div className="flex flex-wrap items-center gap-4">
        <button
          type="submit"
          disabled={status === "submitting"}
          className="rounded-sm border border-amber/45 bg-amber/10 px-6 py-3 font-mono text-[0.8125rem] text-amber transition-colors hover:bg-amber/20 disabled:cursor-not-allowed disabled:opacity-40"
        >
          {status === "submitting" ? "Sending message" : "Send message"}
        </button>

        {/* O verbo do botão sobrevive até a confirmação: Send message → Message sent (DR-09). */}
        <p aria-live="polite" className="m-0 font-mono text-[0.8125rem]">
          {status === "success" && <span className="text-amber">Message sent.</span>}
          {status === "error" && <span className="text-clay">{errorMessage}</span>}
        </p>
      </div>
    </form>
  );
}
