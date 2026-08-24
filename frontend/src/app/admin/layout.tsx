import type { Metadata } from "next";

// A rota existe publicamente na Vercel: quem descobrir a URL vê a tela de senha, e nada além
// dela — a autorização real está na API, não aqui. O que este bloco evita é a rota entrar em
// buscador. Não há link para /admin em nenhuma página pública (guardado por design-system.test).
export const metadata: Metadata = {
  title: "Admin — Pedro Chasci Puga",
  robots: {
    index: false,
    follow: false,
    nocache: true,
    googleBot: { index: false, follow: false },
  },
};

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  return <main className="shell py-16 md:py-24">{children}</main>;
}
