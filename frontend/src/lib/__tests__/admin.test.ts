// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import {
  login,
  logout,
  getSession,
  listProjects,
  createProject,
  updateProject,
  deleteProject,
  type ProjectInput,
} from "../admin";

const projeto: ProjectInput = {
  title: "Portfolio API",
  description: "descrição",
  tags: ["C#"],
  githubUrl: "https://github.com/PedroCPdev/Portfolio",
  liveUrl: "",
  imageUrl: "",
  decisions: [{ title: "primeira", why: "porque sim", cost: "custa" }],
};

function resposta(status: number, body?: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => {
      if (body === undefined) throw new SyntaxError("sem corpo");
      return body;
    },
  } as Response;
}

/** O token que os testes usam como sessão válida. */
const TOKEN = "jwt-de-teste";
const EXPIRA = new Date(Date.now() + 3_600_000).toISOString();

describe("cliente do painel", () => {
  beforeEach(() => {
    sessionStorage.clear();
    vi.restoreAllMocks();
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  async function autenticar() {
    vi.stubGlobal("fetch", vi.fn(async () => resposta(200, { token: TOKEN, expiresAt: EXPIRA })));
    await login("senha");
  }

  // ADM-14 (sucesso): a sessão passa a existir e o token vai nas chamadas seguintes
  it("login com sucesso guarda a sessão", async () => {
    await autenticar();

    expect(getSession()?.token).toBe(TOKEN);
  });

  // ADM-14 (falha): senha errada não guarda nada e devolve o erro da API
  it("login com senha errada não guarda sessão e devolve o erro da API", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => resposta(401, { success: false, error: "Invalid credentials." })));

    const resultado = await login("errada");

    expect(resultado.ok).toBe(false);
    if (!resultado.ok) expect(resultado.error).toBe("Invalid credentials.");
    expect(getSession()).toBeNull();
  });

  // ADM-22: rate limit do login tem mensagem própria — 429 não é "credencial inválida"
  it("login com 429 explica que foi excesso de tentativas", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => resposta(429)));

    const resultado = await login("senha");

    expect(resultado.ok).toBe(false);
    if (!resultado.ok) expect(resultado.error.toLowerCase()).toContain("too many");
  });

  it("logout apaga a sessão", async () => {
    await autenticar();
    logout();

    expect(getSession()).toBeNull();
  });

  // ADM-16: a escrita manda o token e o corpo exato, sem cache
  it("createProject envia Authorization, corpo e cache no-store", async () => {
    await autenticar();
    const fetchMock = vi.fn(async () => resposta(201, { id: "abc", ...projeto, createdAt: EXPIRA }));
    vi.stubGlobal("fetch", fetchMock);

    const resultado = await createProject(projeto);

    expect(resultado.ok).toBe(true);
    const [url, init] = fetchMock.mock.calls[0] as unknown as [string, RequestInit];
    expect(url).toContain("/api/projects");
    expect(init.method).toBe("POST");
    expect((init.headers as Record<string, string>).Authorization).toBe(`Bearer ${TOKEN}`);
    expect(init.cache).toBe("no-store");
    expect(JSON.parse(init.body as string)).toEqual(projeto);
  });

  // ADM-17: a ordem das decisões enviada é a ordem recebida do formulário
  it("createProject preserva a ordem das decisões", async () => {
    await autenticar();
    const fetchMock = vi.fn(async () => resposta(201, {}));
    vi.stubGlobal("fetch", fetchMock);

    await createProject({
      ...projeto,
      decisions: [
        { title: "c", why: "3", cost: "" },
        { title: "a", why: "1", cost: "" },
        { title: "b", why: "2", cost: "" },
      ],
    });

    const [, init] = fetchMock.mock.calls[0] as unknown as [string, RequestInit];
    expect(JSON.parse(init.body as string).decisions.map((d: { title: string }) => d.title)).toEqual(["c", "a", "b"]);
  });

  it("updateProject usa PUT no id certo", async () => {
    await autenticar();
    const fetchMock = vi.fn(async () => resposta(200, {}));
    vi.stubGlobal("fetch", fetchMock);

    await updateProject("id-do-projeto", projeto);

    const [url, init] = fetchMock.mock.calls[0] as unknown as [string, RequestInit];
    expect(url).toContain("/api/projects/id-do-projeto");
    expect(init.method).toBe("PUT");
  });

  // (sem sucesso): 204 não tem corpo — tentar parsear seria erro
  it("deleteProject trata o 204 sem corpo como sucesso", async () => {
    await autenticar();
    vi.stubGlobal("fetch", vi.fn(async () => resposta(204)));

    const resultado = await deleteProject("id-do-projeto");

    expect(resultado.ok).toBe(true);
  });

  // ADM-18 (falha): 401 em qualquer ação derruba a sessão
  it("401 numa escrita apaga a sessão e devolve status 401", async () => {
    await autenticar();
    vi.stubGlobal("fetch", vi.fn(async () => resposta(401, { success: false, error: "Invalid credentials." })));

    const resultado = await createProject(projeto);

    expect(resultado.ok).toBe(false);
    if (!resultado.ok) expect(resultado.status).toBe(401);
    expect(getSession()).toBeNull();
  });

  // ADM-22 (falha): erro do servidor aparece, e NÃO derruba a sessão
  it("500 devolve erro legível e mantém a sessão", async () => {
    await autenticar();
    vi.stubGlobal("fetch", vi.fn(async () => resposta(500, { success: false, error: "boom" })));

    const resultado = await createProject(projeto);

    expect(resultado.ok).toBe(false);
    if (!resultado.ok) {
      expect(resultado.status).toBe(500);
      expect(resultado.error).toBe("boom");
    }
    expect(getSession()?.token).toBe(TOKEN);
  });

  // ADM-22 (falha): rede caída não pode virar sucesso silencioso
  it("falha de rede devolve erro legível em vez de lançar", async () => {
    await autenticar();
    vi.stubGlobal("fetch", vi.fn(async () => {
      throw new TypeError("Failed to fetch");
    }));

    const resultado = await listProjects();

    expect(resultado.ok).toBe(false);
    if (!resultado.ok) expect(resultado.error).toBeTruthy();
  });

  // ADM-13: sem sessão, a escrita nem sai da máquina
  it("escrita sem sessão falha sem chamar a API", async () => {
    const fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);

    const resultado = await createProject(projeto);

    expect(resultado.ok).toBe(false);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  // ADM-18: sessão vencida no relógio é descartada antes de qualquer chamada
  it("sessão expirada é descartada na leitura", async () => {
    vi.stubGlobal("fetch", vi.fn(async () =>
      resposta(200, { token: TOKEN, expiresAt: new Date(Date.now() - 1000).toISOString() })));
    await login("senha");

    expect(getSession()).toBeNull();
  });
});
