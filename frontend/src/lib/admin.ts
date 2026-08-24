import type { Decision, Project } from "./api";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5002";
const SESSION_KEY = "portfolio-admin-session";

/**
 * O que o painel envia. Separado de `Project` porque `id` e `createdAt` são do servidor:
 * mandá-los seria escrever um campo que a API descarta.
 */
export interface ProjectInput {
  title: string;
  description: string;
  tags: string[];
  githubUrl: string;
  liveUrl: string;
  imageUrl: string;
  decisions: Decision[];
}

export interface AdminSession {
  token: string;
  expiresAt: string;
}

/**
 * Resultado discriminado em vez de exceção ou de valor vazio. `getProjects` engole a falha de
 * propósito — na vitrine o pior caso é uma seção vazia. Aqui o pior caso é o admin achar que
 * salvou e não ter salvo, então todo erro precisa chegar à tela (ADM-22).
 */
export type AdminResult<T> =
  | { ok: true; data: T }
  | { ok: false; status: number; error: string };

/** Status usado quando a requisição nem chegou a ter resposta (rede caída, timeout). */
const SEM_RESPOSTA = 0;

const temBrowser = () => typeof window !== "undefined";

/**
 * Assinantes da sessão. `sessionStorage` não dispara evento para a própria aba que escreveu,
 * então quem quiser reagir a login e logout precisa ser avisado daqui.
 */
const ouvintes = new Set<() => void>();

export function subscribeSession(ouvinte: () => void): () => void {
  ouvintes.add(ouvinte);
  return () => ouvintes.delete(ouvinte);
}

/**
 * Instantâneo estável para `useSyncExternalStore`: devolve o token, não o objeto da sessão.
 * Um objeto novo a cada leitura faria o React entender que o estado mudou sempre.
 */
export function getSessionToken(): string | null {
  return getSession()?.token ?? null;
}

function notificar(): void {
  for (const ouvinte of ouvintes) ouvinte();
}

/**
 * Sessão em `sessionStorage`, não em cookie httpOnly: o painel está num site (Vercel) e a API
 * noutro (Render), então o cookie seria third-party e é bloqueado pelo Safari e pelas proteções
 * do Chrome — o login funcionaria num navegador e falharia noutro (design D-12).
 */
export function getSession(): AdminSession | null {
  if (!temBrowser()) return null;

  const bruto = sessionStorage.getItem(SESSION_KEY);
  if (!bruto) return null;

  try {
    const sessao = JSON.parse(bruto) as AdminSession;
    if (!sessao?.token || Date.parse(sessao.expiresAt) <= Date.now()) {
      logout();
      return null;
    }
    return sessao;
  } catch {
    logout();
    return null;
  }
}

export function logout(): void {
  if (temBrowser()) sessionStorage.removeItem(SESSION_KEY);
  notificar();
}

export async function login(password: string): Promise<AdminResult<AdminSession>> {
  const resultado = await requisitar<AdminSession>("/api/admin/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ password }),
  });

  if (!resultado.ok) return resultado;

  if (temBrowser()) sessionStorage.setItem(SESSION_KEY, JSON.stringify(resultado.data));
  notificar();
  // Um token que já nasce vencido não é sessão: getSession devolve null e limpa.
  const sessao = getSession();
  return sessao ? { ok: true, data: sessao } : { ok: false, status: SEM_RESPOSTA, error: "Session expired immediately. Check the clock on this machine." };
}

export async function listProjects(): Promise<AdminResult<Project[]>> {
  return requisitar<Project[]>("/api/projects", { method: "GET" });
}

export async function createProject(input: ProjectInput): Promise<AdminResult<Project>> {
  return autenticado<Project>("/api/projects", "POST", input);
}

export async function updateProject(id: string, input: ProjectInput): Promise<AdminResult<Project>> {
  return autenticado<Project>(`/api/projects/${encodeURIComponent(id)}`, "PUT", input);
}

export async function deleteProject(id: string): Promise<AdminResult<null>> {
  return autenticado<null>(`/api/projects/${encodeURIComponent(id)}`, "DELETE");
}

async function autenticado<T>(path: string, method: string, body?: unknown): Promise<AdminResult<T>> {
  const sessao = getSession();
  // Sem sessão a requisição nem sai: pouparia um 401, mas o ponto é não deixar o painel
  // parecer que tentou salvar quando não havia como.
  if (!sessao) return { ok: false, status: 401, error: "Your session has expired. Sign in again." };

  const resultado = await requisitar<T>(path, {
    method,
    headers: {
      Authorization: `Bearer ${sessao.token}`,
      ...(body === undefined ? {} : { "Content-Type": "application/json" }),
    },
    ...(body === undefined ? {} : { body: JSON.stringify(body) }),
  });

  // 401 só significa "sessão acabou" numa chamada autenticada. No login ele significa senha
  // errada, e derrubar a sessão ali trocaria a mensagem que o usuário precisa ler (ADM-18).
  if (!resultado.ok && resultado.status === 401) {
    logout();
    return { ok: false, status: 401, error: "Your session has expired. Sign in again." };
  }

  return resultado;
}

async function requisitar<T>(path: string, init: RequestInit): Promise<AdminResult<T>> {
  let res: Response;
  try {
    // `no-store` explícito: o boundary de leitura do site usa ISR de 60s, e uma escrita ou uma
    // listagem do painel servida de cache mostraria o estado errado logo depois de salvar.
    res = await fetch(`${API_URL}${path}`, { ...init, cache: "no-store" });
  } catch {
    return { ok: false, status: SEM_RESPOSTA, error: "Could not reach the API. It may be waking up — try again." };
  }

  if (res.status === 429) {
    return { ok: false, status: 429, error: "Too many attempts. Wait a minute and try again." };
  }

  // 204 e 413 não trazem corpo; tentar parsear seria transformar sucesso em erro.
  const corpo = (await res.json().catch(() => null)) as { error?: string } | null;

  if (!res.ok) {
    return {
      ok: false,
      status: res.status,
      error: corpo?.error ?? `Request failed (${res.status}).`,
    };
  }

  return { ok: true, data: (corpo ?? null) as T };
}
