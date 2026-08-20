import { describe, it, expect } from "vitest";
import { readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

// Ancorado no proprio arquivo de teste: o gate nao pode depender de onde o shell esta.
const ROOT = join(dirname(fileURLToPath(import.meta.url)), "..", "..");
const SRC = join(ROOT, "src");
const read = (p: string) => readFileSync(join(ROOT, p), "utf8");

function sourceFiles(dir = SRC, acc: string[] = []): string[] {
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry);
    if (statSync(full).isDirectory()) {
      if (entry !== "__tests__") sourceFiles(full, acc);
    } else if (/\.(tsx?|css)$/.test(entry)) {
      acc.push(full);
    }
  }
  return acc;
}

// DR-01 — toda cor sai de token; nenhum hex da paleta antiga sobrevive em src/.
describe("DR-01 sistema de tokens", () => {
  const TOKENS = ["--ground", "--paper", "--rule", "--ink", "--ink-dim", "--amber", "--clay"];

  it("declara os sete tokens de cor em globals.css", () => {
    const css = read("src/app/globals.css");
    for (const token of TOKENS) {
      expect(css, `token ${token} ausente`).toContain(`${token}:`);
    }
  });

  it("não deixa nenhum hex da paleta antiga em src/", () => {
    const LEGACY = ["#050d1a", "#0d1b2e", "#5ba0f5", "#e8f0fe", "#1a3a6b", "#c8d8f0"];
    const offenders: string[] = [];
    for (const file of sourceFiles()) {
      const body = readFileSync(file, "utf8").toLowerCase();
      for (const hex of LEGACY) {
        if (body.includes(hex)) offenders.push(`${file.replace(SRC, "src")} → ${hex}`);
      }
    }
    expect(offenders, `hex legado encontrado:\n${offenders.join("\n")}`).toEqual([]);
  });
});

// DR-02 — três famílias, três papéis, todas via next/font (sem <link> externo).
describe("DR-02 pareamento tipográfico", () => {
  it("carrega Bricolage Grotesque, Newsreader e JetBrains Mono via next/font", () => {
    const layout = read("src/app/layout.tsx");
    expect(layout).toContain("next/font/google");
    for (const family of ["Bricolage_Grotesque", "Newsreader", "JetBrains_Mono"]) {
      expect(layout, `família ${family} não carregada`).toContain(family);
    }
  });

  // Regressão: com as variáveis no <body>, os tokens declarados em :root referenciavam
  // um --font-* inexistente naquele escopo, viravam inválidos, e a prosa caía no sans
  // padrão — sem erro de build, de lint ou de teste. Só aparecia no pixel.
  it("aplica as variáveis de fonte no <html>, não no <body>", () => {
    const layout = read("src/app/layout.tsx");
    // Comentários mencionam as tags em prosa; o teste olha só para o código.
    const code = layout
      .split("\n")
      .filter((line) => !line.trim().startsWith("//"))
      .join("\n");
    const htmlTag = code.match(/<html\b[\s\S]*?>/)?.[0] ?? "";
    for (const v of ["bricolage.variable", "newsreader.variable", "jetbrainsMono.variable"]) {
      expect(htmlTag, `${v} precisa estar no <html>`).toContain(v);
    }
    expect(code).toMatch(/<body>\{children\}<\/body>/);
  });

  it("não usa mais Outfit e não injeta <link> de fonte", () => {
    const layout = read("src/app/layout.tsx");
    expect(layout).not.toContain("Outfit");
    expect(layout).not.toContain("fonts.googleapis.com");
  });
});

// DR-03 / DR-06 — os tells de template não voltam.
describe("DR-03 e DR-06 tells de template removidos", () => {
  it("não contém o eyebrow '// hello world'", () => {
    for (const file of sourceFiles()) {
      expect(readFileSync(file, "utf8")).not.toContain("// hello world");
    }
  });

  it("não rotula seções com ordinais decorativos", () => {
    for (const file of sourceFiles()) {
      const body = readFileSync(file, "utf8");
      for (const ordinal of ['number="01"', 'number="02"', 'number="03"']) {
        expect(body, `${file} usa ${ordinal}`).not.toContain(ordinal);
      }
    }
  });

  it("não anuncia o cargo genérico como manchete do hero", () => {
    expect(read("src/components/Hero.tsx")).not.toMatch(/>\s*Software Developer\s*</);
  });
});

// DR-07 — foco de teclado sempre visível.
describe("DR-07 foco visível", () => {
  it("define :focus-visible com anel próprio", () => {
    const css = read("src/app/globals.css");
    expect(css).toContain(":focus-visible");
    expect(css).toMatch(/outline[^;]*var\(--amber\)/);
  });
});

// DR-08 — movimento respeita a preferência do sistema.
describe("DR-08 movimento reduzido", () => {
  it("neutraliza animação sob prefers-reduced-motion", () => {
    const css = read("src/app/globals.css");
    expect(css).toContain("prefers-reduced-motion: reduce");
    expect(css).toMatch(/animation-duration:\s*0\.01ms/);
    expect(css).toMatch(/transition-duration:\s*0\.01ms/);
  });
});

// DR-09 — o verbo da ação sobrevive até a confirmação.
describe("DR-09 vocabulário consistente", () => {
  it("mantém o verbo de Send message em Message sent", () => {
    const form = read("src/components/ContactForm.tsx");
    expect(form).toContain("Send message");
    expect(form).toContain("Message sent");
  });

  it("não usa rótulo minúsculo decorativo nos botões", () => {
    const form = read("src/components/ContactForm.tsx");
    expect(form).not.toContain("send message");
    expect(read("src/components/Hero.tsx")).not.toContain("my projects");
  });
});

// DR-05 — o ledger é o elemento-assinatura e degrada sem o campo.
describe("DR-05 trade-off ledger", () => {
  it("marca a linha de custo com o token clay", () => {
    const ledger = read("src/components/Ledger.tsx");
    expect(ledger).toMatch(/text-clay/);
    expect(ledger).toContain("Cost");
  });

  it("renderiza o bloco de custo apenas quando o campo existe", () => {
    const ledger = read("src/components/Ledger.tsx");
    expect(ledger).toMatch(/cost\?:\s*string/);
    expect(ledger, "o bloco de custo precisa ser condicional").toMatch(/\{cost\s*&&/);
  });

  it("liga tradeoff do projeto ao ledger", () => {
    const projects = read("src/components/Projects.tsx");
    expect(projects).toMatch(/cost=\{project\.tradeoff\}/);
  });

  // TR-05: `tradeoff` passou a existir no contrato de /api/projects, então o alias local que
  // estendia o tipo não tem mais razão de ser — manter os dois é como as duas definições divergem.
  it("lê tradeoff do tipo de api.ts, sem alias local", () => {
    expect(read("src/lib/api.ts")).toMatch(/tradeoff\?:\s*string/);
    expect(read("src/components/Projects.tsx")).not.toContain("ProjectRecord");
  });

  it("reserva clay a custo e erro — nenhum outro componente o usa", () => {
    const ALLOWED = new Set(["Ledger.tsx", "ContactForm.tsx"]);
    const offenders = sourceFiles()
      .filter((f) => f.includes("components"))
      .filter((f) => /clay/.test(readFileSync(f, "utf8")))
      .map((f) => f.split("/").pop() as string)
      .filter((name) => !ALLOWED.has(name));
    expect(offenders, `clay usado fora do papel: ${offenders.join(", ")}`).toEqual([]);
  });
});
