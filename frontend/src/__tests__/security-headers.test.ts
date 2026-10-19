import { describe, it, expect } from "vitest";
import nextConfig from "../../next.config";

// C-8: o next.config.ts estava vazio e a Vercel não injeta nenhum destes headers sozinha.
async function headersDaRaiz() {
  const regras = (await nextConfig.headers?.()) ?? [];
  const regra = regras.find((r) => r.source === "/:path*");
  expect(regra, "nenhuma regra de headers cobre todas as rotas").toBeDefined();
  return new Map(regra!.headers.map((h) => [h.key.toLowerCase(), h.value]));
}

function diretiva(csp: string, nome: string) {
  return csp
    .split(";")
    .map((d) => d.trim())
    .find((d) => d.startsWith(`${nome} `));
}

describe("C-8 headers de segurança", () => {
  it("impede o site de ser embutido em frame de outra origem", async () => {
    const h = await headersDaRaiz();
    expect(h.get("x-frame-options")).toBe("DENY");
    expect(diretiva(h.get("content-security-policy")!, "frame-ancestors")).toBe("frame-ancestors 'none'");
  });

  it("desliga o sniffing de tipo e limita o referrer", async () => {
    const h = await headersDaRaiz();
    expect(h.get("x-content-type-options")).toBe("nosniff");
    expect(h.get("referrer-policy")).toBe("strict-origin-when-cross-origin");
  });

  it("deixa o navegador falar só com a própria origem e com a API", async () => {
    const csp = (await headersDaRaiz()).get("content-security-policy")!;
    const api = new URL(process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5002").origin;
    expect(diretiva(csp, "connect-src")).toBe(`connect-src 'self' ${api}`);
    expect(diretiva(csp, "default-src")).toBe("default-src 'self'");
    expect(diretiva(csp, "object-src")).toBe("object-src 'none'");
    expect(diretiva(csp, "base-uri")).toBe("base-uri 'self'");
    expect(diretiva(csp, "form-action")).toBe("form-action 'self'");
  });

  it("só libera eval de script fora de produção", async () => {
    const csp = (await headersDaRaiz()).get("content-security-policy")!;
    expect(diretiva(csp, "script-src")!.includes("'unsafe-eval'")).toBe(process.env.NODE_ENV !== "production");
  });
});
