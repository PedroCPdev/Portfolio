import type { NextConfig } from "next";

// O navegador só chama a API pelo formulário de contato e pelo painel admin. Mesma variável e
// mesmo padrão de src/lib/api.ts, reduzidos à origem porque é isso que o connect-src compara.
const apiOrigin = new URL(process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5002").origin;

// 'unsafe-inline' em script-src porque o App Router injeta scripts inline para hidratar a página.
// A alternativa é nonce por requisição, que obriga toda página a renderizar no servidor a cada
// acesso: a home perderia o ISR de 60s e cada visita viraria uma chamada ao Render (AD-017).
// 'unsafe-eval' só em dev, onde o React usa eval para reconstruir a pilha de erro.
const scriptSrc = ["'self'", "'unsafe-inline'", ...(process.env.NODE_ENV === "production" ? [] : ["'unsafe-eval'"])];

const csp = [
  "default-src 'self'",
  `script-src ${scriptSrc.join(" ")}`,
  "style-src 'self' 'unsafe-inline'",
  "img-src 'self' data: blob:",
  "font-src 'self'",
  `connect-src 'self' ${apiOrigin}`,
  "frame-ancestors 'none'",
  "object-src 'none'",
  "base-uri 'self'",
  "form-action 'self'",
].join("; ");

const nextConfig: NextConfig = {
  async headers() {
    return [
      {
        source: "/:path*",
        headers: [
          { key: "Content-Security-Policy", value: csp },
          // Redundante com frame-ancestors para navegador atual; cobre o que ignora CSP.
          { key: "X-Frame-Options", value: "DENY" },
          { key: "X-Content-Type-Options", value: "nosniff" },
          { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
          { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=()" },
        ],
      },
    ];
  },
};

export default nextConfig;
