const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5002";

/** Uma decisão de engenharia do projeto: o que foi decidido, por quê, e o que custou. */
export interface Decision {
  title: string;
  why: string;
  /** Opcional: decisão sem custo declarado aparece sem a linha de custo. */
  cost?: string;
}

export interface Project {
  id: string;
  title: string;
  description: string;
  tags: string[];
  /** A API sempre envia um array; opcional aqui porque o tipo não pode provar isso.
   *  Quem consome usa `?? []` em vez de checar null. Alimenta só `/projects/{id}`. */
  decisions?: Decision[];
  githubUrl?: string;
  liveUrl?: string;
  imageUrl?: string;
  createdAt: string;
}

// A API vive no free tier do Render, que desliga a instância após 15 min sem tráfego e
// leva ~1 min para religar. Uma única tentativa de 8s falhava nesse intervalo e a seção
// de projetos aparecia vazia. As tentativas abaixo cobrem ~54s no total.
const PROJECTS_ATTEMPT_TIMEOUT_MS = 15_000;
const PROJECTS_RETRY_DELAYS_MS = [3_000, 6_000];

const sleep = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

/**
 * Busca com tolerância a cold start. Devolve a resposta HTTP mesmo quando ela é de erro —
 * quem chama decide o que 4xx/5xx significa — e null quando a rede falhou em todas as tentativas.
 * Erro de aplicação não melhora com repetição, então só timeout/falha de rede é repetido.
 */
async function fetchWithColdStartRetry(path: string): Promise<Response | null> {
  const totalAttempts = PROJECTS_RETRY_DELAYS_MS.length + 1;

  for (let attempt = 0; attempt < totalAttempts; attempt++) {
    try {
      return await fetch(`${API_URL}${path}`, {
        next: { revalidate: 60 },
        signal: AbortSignal.timeout(PROJECTS_ATTEMPT_TIMEOUT_MS),
      });
    } catch {
      // Timeout ou falha de rede: pode ser cold start, então vale insistir.
      const delay = PROJECTS_RETRY_DELAYS_MS[attempt];
      if (delay !== undefined) await sleep(delay);
    }
  }

  return null;
}

export async function getProjects(): Promise<Project[]> {
  const res = await fetchWithColdStartRetry("/api/projects");
  // Devolve vazio em vez de lançar para não quebrar o build nem a página.
  if (!res || !res.ok) return [];
  return await res.json();
}

/** Devolve null quando o projeto não existe (404) ou quando a API não respondeu. */
export async function getProject(id: string): Promise<Project | null> {
  const res = await fetchWithColdStartRetry(`/api/projects/${encodeURIComponent(id)}`);
  if (!res || !res.ok) return null;
  return await res.json();
}

export interface ContactPayload {
  name: string;
  email: string;
  message: string;
}

export interface ContactResult {
  success: boolean;
  error?: string;
}

export async function sendContactMessage(payload: ContactPayload): Promise<ContactResult> {
  try {
    const res = await fetch(`${API_URL}/api/contact`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
      signal: AbortSignal.timeout(30000),
    });

    if (res.status === 503) {
      return { success: false, error: "Too many messages sent. Try again in a minute." };
    }

    const data: { error?: string } | null = await res.json().catch(() => null);

    if (!res.ok) {
      return { success: false, error: data?.error ?? "Failed to send message. Try again later." };
    }
    return { success: true };
  } catch {
    return { success: false, error: "Could not reach the server. Try again later." };
  }
}
