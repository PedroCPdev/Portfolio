import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { getProjects } from "../api";

const okProject = {
  id: "abc123",
  title: "Portfolio API",
  description: "d",
  tags: ["C#"],
  createdAt: "2026-01-01T00:00:00Z",
};

function jsonResponse(body: unknown) {
  return { ok: true, status: 200, json: async () => body } as Response;
}

describe("getProjects", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });
  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  // Roda as promises pendentes avançando os timers do backoff até a chamada resolver.
  async function resolveWithTimers<T>(promise: Promise<T>): Promise<T> {
    const settled = promise.then(
      (v) => ({ ok: true, v }) as const,
      (e) => ({ ok: false, e }) as const,
    );
    for (let i = 0; i < 20; i++) {
      await vi.advanceTimersByTimeAsync(10_000);
    }
    const r = await settled;
    if (!r.ok) throw r.e;
    return r.v;
  }

  it("RC-02: tenta novamente quando a primeira chamada falha e devolve os dados da segunda", async () => {
    const fetchMock = vi
      .fn()
      .mockRejectedValueOnce(new Error("timeout"))
      .mockResolvedValueOnce(jsonResponse([okProject]));
    vi.stubGlobal("fetch", fetchMock);

    const result = await resolveWithTimers(getProjects());

    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(result).toEqual([okProject]);
  });

  it("RC-02: cobre um cold start longo — sucesso só na terceira tentativa", async () => {
    const fetchMock = vi
      .fn()
      .mockRejectedValueOnce(new Error("timeout"))
      .mockRejectedValueOnce(new Error("timeout"))
      .mockResolvedValueOnce(jsonResponse([okProject]));
    vi.stubGlobal("fetch", fetchMock);

    const result = await resolveWithTimers(getProjects());

    expect(fetchMock).toHaveBeenCalledTimes(3);
    expect(result).toEqual([okProject]);
  });

  it("RC-03: devolve lista vazia (sem lançar) quando todas as tentativas falham", async () => {
    const fetchMock = vi.fn().mockRejectedValue(new Error("timeout"));
    vi.stubGlobal("fetch", fetchMock);

    const result = await resolveWithTimers(getProjects());

    expect(result).toEqual([]);
    expect(fetchMock.mock.calls.length).toBeGreaterThanOrEqual(3);
  });

  it("não repete quando a resposta chega de primeira", async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse([okProject]));
    vi.stubGlobal("fetch", fetchMock);

    const result = await resolveWithTimers(getProjects());

    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(result).toEqual([okProject]);
  });

  it("RC-03: resposta HTTP de erro não vira retry infinito nem lança", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue({ ok: false, status: 500, json: async () => ({}) } as Response);
    vi.stubGlobal("fetch", fetchMock);

    const result = await resolveWithTimers(getProjects());

    expect(result).toEqual([]);
  });
});
