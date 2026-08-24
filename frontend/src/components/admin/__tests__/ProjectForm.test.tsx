// @vitest-environment jsdom
import { it, expect, vi, afterEach, beforeEach } from "vitest";
import { render, screen, cleanup, fireEvent, waitFor } from "@testing-library/react";
import ProjectForm from "../ProjectForm";
import type { Project } from "@/lib/api";

const createProject = vi.fn();
const updateProject = vi.fn();

vi.mock("@/lib/admin", () => ({
  createProject: (...args: unknown[]) => createProject(...args),
  updateProject: (...args: unknown[]) => updateProject(...args),
}));

beforeEach(() => {
  createProject.mockReset();
  updateProject.mockReset();
});
afterEach(cleanup);

const existente: Project = {
  id: "id-do-projeto",
  title: "Portfolio API",
  description: "descrição atual",
  tags: ["C#", "Firestore"],
  githubUrl: "https://github.com/PedroCPdev/Portfolio",
  liveUrl: "https://exemplo.com",
  imageUrl: "",
  decisions: [
    { title: "primeira", why: "p1", cost: "c1" },
    { title: "segunda", why: "p2", cost: "" },
  ],
  createdAt: "2026-01-01T00:00:00Z",
};

const preencher = (rotulo: RegExp, valor: string) =>
  fireEvent.change(screen.getByLabelText(rotulo), { target: { value: valor } });

// ADM-16 (sucesso)
it("envia o payload exato ao criar", async () => {
  createProject.mockResolvedValue({ ok: true, data: {} });
  render(<ProjectForm onSaved={vi.fn()} onCancel={vi.fn()} />);

  preencher(/^title/i, "Projeto novo");
  preencher(/^description/i, "descrição nova");
  preencher(/^tags/i, "C#, Firestore ,  Docker");
  preencher(/^github/i, "https://github.com/PedroCPdev/Portfolio");
  fireEvent.click(screen.getByRole("button", { name: /add decision/i }));
  preencher(/^what was decided/i, "só uma decisão");
  preencher(/^why/i, "porque sim");
  fireEvent.click(screen.getByRole("button", { name: /save/i }));

  await waitFor(() => expect(createProject).toHaveBeenCalledTimes(1));
  expect(createProject).toHaveBeenCalledWith({
    title: "Projeto novo",
    description: "descrição nova",
    tags: ["C#", "Firestore", "Docker"],
    githubUrl: "https://github.com/PedroCPdev/Portfolio",
    liveUrl: "",
    imageUrl: "",
    decisions: [{ title: "só uma decisão", why: "porque sim", cost: "" }],
  });
});

// ADM-20: editar vem preenchido com o que existe hoje, decisões inclusive
it("no modo edição os campos chegam preenchidos e o envio é PUT no id", async () => {
  updateProject.mockResolvedValue({ ok: true, data: {} });
  render(<ProjectForm project={existente} onSaved={vi.fn()} onCancel={vi.fn()} />);

  expect((screen.getByLabelText(/^title/i) as HTMLInputElement).value).toBe("Portfolio API");
  expect((screen.getByLabelText(/^description/i) as HTMLTextAreaElement).value).toBe("descrição atual");
  expect((screen.getByLabelText(/^tags/i) as HTMLInputElement).value).toBe("C#, Firestore");
  expect(
    (screen.getAllByLabelText(/^what was decided/i) as HTMLInputElement[]).map((i) => i.value),
  ).toEqual(["primeira", "segunda"]);

  fireEvent.click(screen.getByRole("button", { name: /save/i }));

  await waitFor(() => expect(updateProject).toHaveBeenCalledTimes(1));
  expect(updateProject.mock.calls[0][0]).toBe("id-do-projeto");
  expect(updateProject.mock.calls[0][1]).toMatchObject({ title: "Portfolio API", decisions: existente.decisions });
});

// ADM-16: sucesso avisa quem chamou, para a lista se atualizar
it("sucesso chama onSaved", async () => {
  createProject.mockResolvedValue({ ok: true, data: {} });
  const onSaved = vi.fn();
  render(<ProjectForm onSaved={onSaved} onCancel={vi.fn()} />);

  preencher(/^title/i, "Projeto");
  preencher(/^description/i, "descrição");
  fireEvent.click(screen.getByRole("button", { name: /save/i }));

  await waitFor(() => expect(onSaved).toHaveBeenCalledTimes(1));
});

// ADM-22 (falha): o erro da API aparece e o que foi digitado continua lá
it("erro da API aparece na tela sem apagar o formulário", async () => {
  createProject.mockResolvedValue({ ok: false, status: 400, error: "Title cannot be blank." });
  const onSaved = vi.fn();
  render(<ProjectForm onSaved={onSaved} onCancel={vi.fn()} />);

  preencher(/^title/i, "Projeto que falha");
  preencher(/^description/i, "descrição");
  fireEvent.click(screen.getByRole("button", { name: /save/i }));

  await waitFor(() => expect(screen.getByRole("alert").textContent).toContain("Title cannot be blank."));
  expect((screen.getByLabelText(/^title/i) as HTMLInputElement).value).toBe("Projeto que falha");
  expect(onSaved).not.toHaveBeenCalled();
});

// ADM-16 (falha): dois cliques não podem virar dois projetos
it("o botão de salvar desabilita durante o envio", async () => {
  let liberar: (v: unknown) => void = () => {};
  createProject.mockReturnValue(new Promise((resolve) => { liberar = resolve; }));
  render(<ProjectForm onSaved={vi.fn()} onCancel={vi.fn()} />);

  preencher(/^title/i, "Projeto");
  preencher(/^description/i, "descrição");
  const salvar = screen.getByRole("button", { name: /save/i }) as HTMLButtonElement;
  fireEvent.click(salvar);

  await waitFor(() => expect(salvar.disabled).toBe(true));
  fireEvent.click(salvar);
  expect(createProject).toHaveBeenCalledTimes(1);

  liberar({ ok: true, data: {} });
});

// ADM-16: cancelar não salva nada
it("cancelar chama onCancel sem enviar", () => {
  const onCancel = vi.fn();
  render(<ProjectForm onSaved={vi.fn()} onCancel={onCancel} />);

  fireEvent.click(screen.getByRole("button", { name: /cancel/i }));

  expect(onCancel).toHaveBeenCalledTimes(1);
  expect(createProject).not.toHaveBeenCalled();
});
