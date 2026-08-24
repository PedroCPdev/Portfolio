// @vitest-environment jsdom
import { it, expect, vi, afterEach, beforeEach } from "vitest";
import { render, screen, cleanup, fireEvent, waitFor } from "@testing-library/react";
import AdminPanel from "../AdminPanel";
import type { Project } from "@/lib/api";

// A sessão é um store externo (useSyncExternalStore), então o dublê precisa ser um store de
// verdade: um token e assinantes que são avisados quando ele muda.
let tokenAtual: string | null = null;
const assinantes = new Set<() => void>();
const notificar = () => assinantes.forEach((f) => f());

const comSessao = () => {
  tokenAtual = "jwt-de-teste";
  notificar();
};

const login = vi.fn();
const logout = vi.fn(() => {
  tokenAtual = null;
  notificar();
});
const listProjects = vi.fn();
const deleteProject = vi.fn();

vi.mock("@/lib/admin", () => ({
  getSessionToken: () => tokenAtual,
  subscribeSession: (ouvinte: () => void) => {
    assinantes.add(ouvinte);
    return () => assinantes.delete(ouvinte);
  },
  login: (senha: string) => login(senha),
  logout: () => logout(),
  listProjects: () => listProjects(),
  deleteProject: (id: string) => deleteProject(id),
  createProject: vi.fn(),
  updateProject: vi.fn(),
}));

const projeto: Project = {
  id: "id-1",
  title: "Portfolio API",
  description: "descrição",
  tags: ["C#"],
  decisions: [{ title: "uma", why: "porque", cost: "" }],
  createdAt: "2026-01-01T00:00:00Z",
};

beforeEach(() => {
  for (const m of [login, listProjects, deleteProject]) m.mockReset();
  logout.mockClear();
  tokenAtual = null;
  assinantes.clear();
  listProjects.mockResolvedValue({ ok: true, data: [] });
});
afterEach(cleanup);

const senhaInput = () => screen.getByLabelText(/password/i);

// ADM-13 (sucesso): sem sessão não existe painel, só a porta
it("sem sessão mostra apenas o login", () => {
  tokenAtual = null;

  render(<AdminPanel />);

  expect(senhaInput()).toBeDefined();
  expect(screen.queryByRole("button", { name: /new project/i })).toBeNull();
  expect(listProjects).not.toHaveBeenCalled();
});

// ADM-14 (falha): senha errada mostra o erro e não entra
it("senha errada mostra o erro e continua no login", async () => {
  tokenAtual = null;
  login.mockResolvedValue({ ok: false, status: 401, error: "Invalid credentials." });

  render(<AdminPanel />);
  fireEvent.change(senhaInput(), { target: { value: "errada" } });
  fireEvent.click(screen.getByRole("button", { name: /sign in/i }));

  await waitFor(() => expect(screen.getByRole("alert").textContent).toContain("Invalid credentials."));
  expect(screen.queryByRole("button", { name: /new project/i })).toBeNull();
});

// ADM-13 (sucesso): senha certa entra — o painel reage ao token aparecer no store da sessão
it("login com sucesso troca a tela de senha pela lista", async () => {
  tokenAtual = null;
  listProjects.mockResolvedValue({ ok: true, data: [projeto] });
  login.mockImplementation(async () => {
    comSessao();
    return { ok: true, data: { token: "jwt-de-teste", expiresAt: "2099-01-01T00:00:00Z" } };
  });

  render(<AdminPanel />);
  fireEvent.change(senhaInput(), { target: { value: "senha-certa" } });
  fireEvent.click(screen.getByRole("button", { name: /sign in/i }));

  await waitFor(() => expect(screen.getByText("Portfolio API")).toBeDefined());
  expect(screen.queryByLabelText(/password/i)).toBeNull();
});

// ADM-15 (sucesso): autenticado, a lista aparece com as ações
it("com sessão lista os projetos com editar e excluir", async () => {
  comSessao();
  listProjects.mockResolvedValue({ ok: true, data: [projeto] });

  render(<AdminPanel />);

  await waitFor(() => expect(screen.getByText("Portfolio API")).toBeDefined());
  expect(screen.getByRole("button", { name: /edit/i })).toBeDefined();
  expect(screen.getByRole("button", { name: /delete/i })).toBeDefined();
  expect(screen.getByRole("button", { name: /new project/i })).toBeDefined();
});

// ADM-15 (sem sucesso): nenhum projeto é estado legítimo, não erro
it("com sessão e nenhum projeto mostra o estado vazio e o botão de criar", async () => {
  comSessao();

  render(<AdminPanel />);

  await waitFor(() => expect(screen.getByText(/no projects yet/i)).toBeDefined());
  expect(screen.getByRole("button", { name: /new project/i })).toBeDefined();
});

// ADM-22: falha ao carregar aparece, não vira lista vazia silenciosa
it("erro ao carregar a lista aparece na tela", async () => {
  comSessao();
  listProjects.mockResolvedValue({ ok: false, status: 500, error: "boom" });

  render(<AdminPanel />);

  await waitFor(() => expect(screen.getByRole("alert").textContent).toContain("boom"));
});

// ADM-18 (falha): 401 durante uma ação derruba a sessão e avisa
it("401 numa ação volta ao login com aviso de sessão expirada", async () => {
  comSessao();
  listProjects.mockResolvedValue({ ok: false, status: 401, error: "Your session has expired. Sign in again." });

  render(<AdminPanel />);

  await waitFor(() => expect(senhaInput()).toBeDefined());
  expect(screen.getByRole("alert").textContent).toMatch(/expired/i);
  expect(screen.queryByRole("button", { name: /new project/i })).toBeNull();
});

// ADM-16: o formulário de criação abre a partir da lista
it("new project abre o formulário vazio", async () => {
  comSessao();
  listProjects.mockResolvedValue({ ok: true, data: [projeto] });

  render(<AdminPanel />);
  await waitFor(() => expect(screen.getByText("Portfolio API")).toBeDefined());
  fireEvent.click(screen.getByRole("button", { name: /new project/i }));

  expect((screen.getByLabelText(/^title/i) as HTMLInputElement).value).toBe("");
});

// ADM-20: editar abre o formulário preenchido
it("edit abre o formulário com os valores do projeto", async () => {
  comSessao();
  listProjects.mockResolvedValue({ ok: true, data: [projeto] });

  render(<AdminPanel />);
  await waitFor(() => expect(screen.getByText("Portfolio API")).toBeDefined());
  fireEvent.click(screen.getByRole("button", { name: /edit/i }));

  expect((screen.getByLabelText(/^title/i) as HTMLInputElement).value).toBe("Portfolio API");
});

// ADM-21: excluir passa pela confirmação por digitação antes de chamar a API
it("delete só chama a API depois da confirmação por título", async () => {
  comSessao();
  listProjects.mockResolvedValue({ ok: true, data: [projeto] });
  deleteProject.mockResolvedValue({ ok: true, data: null });

  render(<AdminPanel />);
  await waitFor(() => expect(screen.getByText("Portfolio API")).toBeDefined());
  fireEvent.click(screen.getByRole("button", { name: /delete/i }));

  expect(deleteProject).not.toHaveBeenCalled();

  fireEvent.change(screen.getByLabelText(/type the title/i), { target: { value: "Portfolio API" } });
  fireEvent.click(screen.getByRole("button", { name: /delete project/i }));

  await waitFor(() => expect(deleteProject).toHaveBeenCalledWith("id-1"));
});

// ADM-13: sair descarta a sessão e volta para a porta
it("sign out limpa a sessão e volta ao login", async () => {
  comSessao();
  listProjects.mockResolvedValue({ ok: true, data: [projeto] });

  render(<AdminPanel />);
  await waitFor(() => expect(screen.getByText("Portfolio API")).toBeDefined());
  fireEvent.click(screen.getByRole("button", { name: /sign out/i }));

  expect(logout).toHaveBeenCalledTimes(1);
  expect(senhaInput()).toBeDefined();
});
