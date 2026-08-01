"use client";

import { useState, FormEvent } from "react";
import { sendContactMessage } from "@/lib/api";

const inputClass =
  "bg-[#050d1a] border-[0.5px] border-[#5ba0f5]/[0.14] rounded-lg px-4 py-2.5 text-[13px] text-[#e8f0fe] placeholder:text-[#e8f0fe]/25 focus:outline-none focus:border-[#5ba0f5]/40 transition-colors";

type Status = "idle" | "submitting" | "success" | "error";

export default function ContactForm() {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState("");
  const [status, setStatus] = useState<Status>("idle");
  const [errorMessage, setErrorMessage] = useState("");

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setStatus("submitting");

    const result = await sendContactMessage({ name, email, message });

    if (result.success) {
      setStatus("success");
      setName("");
      setEmail("");
      setMessage("");
    } else {
      setStatus("error");
      setErrorMessage(result.error ?? "Failed to send message.");
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4 w-full">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <input
          type="text"
          required
          placeholder="Your name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          className={inputClass}
        />
        <input
          type="email"
          required
          placeholder="Your email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className={inputClass}
        />
      </div>
      <textarea
        required
        rows={4}
        placeholder="Your message"
        value={message}
        onChange={(e) => setMessage(e.target.value)}
        className={`${inputClass} resize-none`}
      />
      <div className="flex items-center gap-4">
        <button
          type="submit"
          disabled={status === "submitting"}
          className="inline-flex items-center gap-1.5 py-2.5 px-6 rounded-lg text-[13px] border-[0.5px] border-[#5ba0f5]/30 bg-[#5ba0f5]/6 text-[#5ba0f5] transition-all duration-200 hover:bg-[#5ba0f5]/10 disabled:opacity-40 disabled:cursor-not-allowed"
        >
          {status === "submitting" ? "sending..." : "send message"}
        </button>
        {status === "success" && (
          <span className="text-[13px] text-[#5ba0f5]">Message sent — thanks!</span>
        )}
        {status === "error" && (
          <span className="text-[13px] text-[#f87171]">{errorMessage}</span>
        )}
      </div>
    </form>
  );
}
