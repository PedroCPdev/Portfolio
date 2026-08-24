import { defineConfig } from "vitest/config";
import { fileURLToPath } from "node:url";

// Só o alias. O ambiente continua sendo `node` por padrão e cada teste de componente liga o
// jsdom no próprio arquivo com `// @vitest-environment jsdom` — assim os testes que leem o
// código-fonte (design-system) e os de `lib/` não pagam o custo do DOM.
//
// Existe porque os componentes importam `@/lib/...` como o resto do site (ContactForm faz o
// mesmo). Sem o alias, o vitest só resolvia import de tipo, que some no transform.
export default defineConfig({
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
});
