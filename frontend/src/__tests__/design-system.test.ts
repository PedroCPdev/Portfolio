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

// PD-01 / PD-05 — o registro de decisão é o elemento-assinatura, agora no detalhe.
describe("PD-05 registro de decisão", () => {
  it("mostra o porquê e marca o custo com o token clay", () => {
    const rec = read("src/components/DecisionRecord.tsx");
    expect(rec).toContain("Why");
    expect(rec).toContain("Cost");
    expect(rec).toMatch(/text-clay/);
  });

  it("renderiza o bloco de custo apenas quando o campo existe", () => {
    const rec = read("src/components/DecisionRecord.tsx");
    expect(rec, "o bloco de custo precisa ser condicional").toMatch(/\{decision\.cost\s*&&/);
  });

  it("reserva clay a custo e erro — nenhum outro componente o usa", () => {
    const ALLOWED = new Set(["DecisionRecord.tsx", "ContactForm.tsx"]);
    const offenders = sourceFiles()
      .filter((f) => f.includes("components"))
      .filter((f) => /clay/.test(readFileSync(f, "utf8")))
      .map((f) => f.split("/").pop() as string)
      .filter((name) => !ALLOWED.has(name));
    expect(offenders, `clay usado fora do papel: ${offenders.join(", ")}`).toEqual([]);
  });
});

// PD-03 — a home voltou a ser vitrine; o raciocínio mora na página de detalhe.
describe("PD-03 home não carrega decisão", () => {
  it("não renderiza decisão nem custo na seção de projetos", () => {
    const projects = read("src/components/Projects.tsx");
    expect(projects).not.toContain("DecisionRecord");
    expect(projects).not.toMatch(/>\s*Cost\s*</);
  });

  it("não renderiza decisão nem custo no hero", () => {
    const hero = read("src/components/Hero.tsx");
    expect(hero).not.toContain("DecisionRecord");
    expect(hero).not.toContain("Ledger");
    expect(hero).not.toMatch(/>\s*(Kept|Cost)\s*</);
  });
});

// PD-04 / PD-06 / PD-07 — o card leva a uma URL própria, e a rota trata ausência.
describe("PD-04 navegação para o detalhe", () => {
  it("o card aponta para /projects/{id}", () => {
    const projects = read("src/components/Projects.tsx");
    expect(projects).toMatch(/href=\{`\/projects\/\$\{project\.id\}`\}/);
  });

  it("a rota de detalhe existe e usa notFound para id inexistente", () => {
    const page = read("src/app/projects/[id]/page.tsx");
    expect(page).toContain("next/navigation");
    expect(page).toMatch(/if\s*\(!project\)\s*notFound\(\)/);
  });

  it("a rota de detalhe trata projeto sem decisões", () => {
    const page = read("src/app/projects/[id]/page.tsx");
    expect(page).toMatch(/decisions\.length === 0/);
  });
});

// PD-08 — largura e grid acompanham a tela; a prosa não.
describe("PD-08 responsividade", () => {
  it("define o contêiner .shell e a medida de leitura .measure", () => {
    const css = read("src/app/globals.css");
    expect(css).toMatch(/\.shell\s*\{/);
    expect(css).toMatch(/\.measure\s*\{/);
    expect(css).toMatch(/max-width:\s*65ch/);
  });

  it("o grid de projetos ganha colunas conforme a viewport", () => {
    const projects = read("src/components/Projects.tsx");
    expect(projects).toContain("sm:grid-cols-2");
    expect(projects).toContain("xl:grid-cols-3");
  });

  it("nenhum componente prende a página na largura antiga", () => {
    for (const file of sourceFiles()) {
      expect(readFileSync(file, "utf8"), `${file} ainda usa max-w-5xl`).not.toContain("max-w-5xl");
    }
  });
});

// PD-01 — o contrato do frontend acompanha o da API.
describe("PD-01 contrato de decisões no frontend", () => {
  it("api.ts expõe Decision e o campo decisions, e não expõe mais tradeoff", () => {
    const api = read("src/lib/api.ts");
    expect(api).toMatch(/export interface Decision/);
    expect(api).toMatch(/decisions\?:\s*Decision\[\]/);
    expect(api).not.toMatch(/tradeoff/);
  });

  it("api.ts expõe getProject para a página de detalhe", () => {
    expect(read("src/lib/api.ts")).toMatch(/export async function getProject\(/);
  });
});
