"use client";

import { useState, FormEvent } from "react";
import type { Decision, Project } from "@/lib/api";
import { createProject, updateProject, type ProjectInput } from "@/lib/admin";
import DecisionEditor from "./DecisionEditor";

const fieldClass =
  "w-full rounded-sm border border-rule bg-paper px-4 py-3 font-body text-[0.9375rem] text-ink placeholder:text-ink-dim/60 transition-colors focus:border-amber/50";

const labelClass = "label mb-2 block";

interface Props {
  /** Ausente = criação. Presente = edição, e os campos chegam com os valores atuais (ADM-20). */
  project?: Project;
  onSaved: () => void;
  onCancel: () => void;
}

export default function ProjectForm({ project, onSaved, onCancel }: Props) {
  const [title, setTitle] = useState(project?.title ?? "");
  const [description, setDescription] = useState(project?.description ?? "");
  const [tags, setTags] = useState((project?.tags ?? []).join(", "));
  const [githubUrl, setGithubUrl] = useState(project?.githubUrl ?? "");
  const [liveUrl, setLiveUrl] = useState(project?.liveUrl ?? "");
  const [imageUrl, setImageUrl] = useState(project?.imageUrl ?? "");
  const [decisions, setDecisions] = useState<Decision[]>(project?.decisions ?? []);
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState("");

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (enviando) return;

    setEnviando(true);
    setErro("");

    const entrada: ProjectInput = {
      title,
      description,
      // Tag vazia entre vírgulas é digitação, não dado — a API recusaria o item em branco.
      tags: tags.split(",").map((t) => t.trim()).filter(Boolean),
      githubUrl,
      liveUrl,
      imageUrl,
      decisions,
    };

    const resultado = project
      ? await updateProject(project.id, entrada)
      : await createProject(entrada);

    setEnviando(false);

    // Nada é limpo no erro: o texto digitado é o trabalho do usuário, e apagá-lo para
    // mostrar uma mensagem seria cobrar duas vezes pelo mesmo formulário (ADM-22).
    if (!resultado.ok) {
      setErro(resultado.error);
      return;
    }

    onSaved();
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      <div>
        <label className={labelClass} htmlFor="title">
          Title
        </label>
        <input
          id="title"
          className={fieldClass}
          value={title}
          maxLength={120}
          onChange={(e) => setTitle(e.target.value)}
        />
      </div>

      <div>
        <label className={labelClass} htmlFor="description">
          Description
        </label>
        <textarea
          id="description"
          className={`${fieldClass} min-h-32 resize-y`}
          value={description}
          maxLength={2000}
          onChange={(e) => setDescription(e.target.value)}
        />
      </div>

      <div>
        <label className={labelClass} htmlFor="tags">
          Tags <span className="normal-case">(comma separated, up to 12)</span>
        </label>
        <input
          id="tags"
          className={fieldClass}
          value={tags}
          onChange={(e) => setTags(e.target.value)}
        />
      </div>

      <div className="grid grid-cols-1 gap-5 sm:grid-cols-3">
        <div>
          <label className={labelClass} htmlFor="githubUrl">
            GitHub URL
          </label>
          <input
            id="githubUrl"
            className={fieldClass}
            value={githubUrl}
            onChange={(e) => setGithubUrl(e.target.value)}
          />
        </div>
        <div>
          <label className={labelClass} htmlFor="liveUrl">
            Live URL
          </label>
          <input
            id="liveUrl"
            className={fieldClass}
            value={liveUrl}
            onChange={(e) => setLiveUrl(e.target.value)}
          />
        </div>
        <div>
          <label className={labelClass} htmlFor="imageUrl">
            Image URL
          </label>
          <input
            id="imageUrl"
            className={fieldClass}
            value={imageUrl}
            onChange={(e) => setImageUrl(e.target.value)}
          />
        </div>
      </div>

      <div className="border-t border-rule pt-6">
        <DecisionEditor decisions={decisions} onChange={setDecisions} />
      </div>

      {erro && (
        <p role="alert" className="font-body text-[0.9375rem] text-clay">
          {erro}
        </p>
      )}

      <div className="flex flex-wrap items-center gap-3 border-t border-rule pt-6">
        <button
          type="submit"
          disabled={enviando}
          className="label rounded-sm border border-amber/60 bg-amber/10 px-4 py-2 text-ink transition-colors hover:bg-amber/20 disabled:cursor-not-allowed disabled:opacity-40"
        >
          {enviando ? "Saving…" : "Save project"}
        </button>
        <button
          type="button"
          onClick={onCancel}
          className="label rounded-sm border border-rule px-4 py-2 transition-colors hover:border-amber/50 hover:text-ink"
        >
          Cancel
        </button>
      </div>
    </form>
  );
}
