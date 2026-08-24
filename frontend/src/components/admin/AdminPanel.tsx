"use client";

import { useCallback, useEffect, useState, useSyncExternalStore } from "react";
import type { Project } from "@/lib/api";
import {
  deleteProject,
  getSessionToken,
  listProjects,
  login,
  logout,
  subscribeSession,
  type AdminResult,
} from "@/lib/admin";
import LoginForm from "./LoginForm";
import ProjectForm from "./ProjectForm";
import DeleteConfirm from "./DeleteConfirm";

type Vista =
  | { nome: "lista" }
  | { nome: "criando" }
  | { nome: "editando"; projeto: Project }
  | { nome: "excluindo"; projeto: Project };

export default function AdminPanel() {
  // A sessão mora no sessionStorage, que só existe no browser: no render do servidor o
  // instantâneo é sempre null, e o valor real chega na hidratação sem setState em efeito.
  const token = useSyncExternalStore(subscribeSession, getSessionToken, () => null);
  const autenticado = token !== null;

  // null = ainda não carregou. Distinguir isso de [] é o que separa "carregando" de
  // "não há projeto nenhum" — dois estados que a mesma lista vazia confundiria.
  const [projetos, setProjetos] = useState<Project[] | null>(null);
  const [vista, setVista] = useState<Vista>({ nome: "lista" });
  const [erro, setErro] = useState("");

  /**
   * Toda resposta da API passa por aqui. É o único lugar que decide o que fazer com 401:
   * a sessão morreu, então o painel volta para a porta em vez de mostrar telas que não
   * funcionam mais (ADM-18).
   */
  const tratar = useCallback(<T,>(resultado: AdminResult<T>): resultado is { ok: true; data: T } => {
    if (resultado.ok) {
      setErro("");
      return true;
    }

    setErro(resultado.error);
    if (resultado.status === 401) {
      logout();
      setProjetos(null);
      setVista({ nome: "lista" });
    }
    return false;
  }, []);

  const carregar = useCallback(async () => {
    const resultado = await listProjects();
    if (tratar(resultado)) setProjetos(resultado.data);
  }, [tratar]);

  // A lista é carregada num callback da promessa, não no corpo do efeito: o efeito só dispara
  // o pedido e a resposta é o "sistema externo" que traz o estado de volta. `ativo` evita
  // gravar estado depois de o painel sair da tela.
  useEffect(() => {
    if (!autenticado) return;

    let ativo = true;
    listProjects().then((resultado) => {
      if (!ativo) return;
      if (tratar(resultado)) setProjetos(resultado.data);
    });

    return () => {
      ativo = false;
    };
  }, [autenticado, tratar]);

  async function entrar(password: string) {
    const resultado = await login(password);
    // O `autenticado` sai do store da sessão: `login` avisa os assinantes ao gravar o token.
    setErro(resultado.ok ? "" : resultado.error);
  }

  function sair() {
    logout();
    setProjetos(null);
    setErro("");
    setVista({ nome: "lista" });
  }

  async function excluir(projeto: Project) {
    const resultado = await deleteProject(projeto.id);
    if (!tratar(resultado)) return;

    setVista({ nome: "lista" });
    await carregar();
  }

  async function salvou() {
    setVista({ nome: "lista" });
    await carregar();
  }

  if (!autenticado) {
    return (
      <div className="flex flex-col gap-6">
        <header>
          <p className="label mb-2">Admin</p>
          <h1 className="font-display text-3xl text-ink">Sign in to manage projects</h1>
        </header>
        <LoginForm onSubmit={entrar} error={erro} />
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-8">
      <header className="flex flex-wrap items-baseline justify-between gap-4 border-b border-rule pb-6">
        <div>
          <p className="label mb-2">Admin</p>
          <h1 className="font-display text-3xl text-ink">Projects</h1>
        </div>
        <div className="flex flex-wrap gap-3">
          {vista.nome === "lista" && (
            <button
              type="button"
              onClick={() => setVista({ nome: "criando" })}
              className="label rounded-sm border border-amber/60 bg-amber/10 px-4 py-2 text-ink transition-colors hover:bg-amber/20"
            >
              + New project
            </button>
          )}
          <button
            type="button"
            onClick={sair}
            className="label rounded-sm border border-rule px-4 py-2 transition-colors hover:border-amber/50 hover:text-ink"
          >
            Sign out
          </button>
        </div>
      </header>

      {erro && (
        <p role="alert" className="font-body text-[0.9375rem] text-clay">
          {erro}
        </p>
      )}

      {vista.nome === "criando" && (
        <ProjectForm onSaved={salvou} onCancel={() => setVista({ nome: "lista" })} />
      )}

      {vista.nome === "editando" && (
        <ProjectForm
          project={vista.projeto}
          onSaved={salvou}
          onCancel={() => setVista({ nome: "lista" })}
        />
      )}

      {vista.nome === "excluindo" && (
        <DeleteConfirm
          title={vista.projeto.title}
          onConfirm={() => void excluir(vista.projeto)}
          onCancel={() => setVista({ nome: "lista" })}
        />
      )}

      {vista.nome === "lista" && (
        <>
          {projetos === null && (
            <p className="font-body text-[0.9375rem] text-ink-dim">Loading…</p>
          )}

          {projetos?.length === 0 && (
            <p className="font-body text-[0.9375rem] text-ink-dim measure">
              No projects yet. The site shows an empty section until the first one exists.
            </p>
          )}

          <ul className="flex flex-col divide-y divide-rule border-y border-rule">
            {(projetos ?? []).map((projeto) => (
              <li key={projeto.id} className="flex flex-wrap items-baseline justify-between gap-4 py-5">
                <div className="measure">
                  <h2 className="font-display text-lg text-ink">{projeto.title}</h2>
                  <p className="mt-1 font-body text-[0.9375rem] text-ink-dim">{projeto.description}</p>
                  <p className="label mt-2">
                    {projeto.id} · {(projeto.decisions ?? []).length} decisions
                  </p>
                </div>
                <div className="flex gap-3">
                  <button
                    type="button"
                    onClick={() => setVista({ nome: "editando", projeto })}
                    className="label rounded-sm border border-rule px-3 py-2 transition-colors hover:border-amber/50 hover:text-ink"
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    onClick={() => setVista({ nome: "excluindo", projeto })}
                    className="label rounded-sm border border-rule px-3 py-2 transition-colors hover:border-clay/60 hover:text-clay"
                  >
                    Delete
                  </button>
                </div>
              </li>
            ))}
          </ul>
        </>
      )}
    </div>
  );
}
