import type { Metadata } from "next";
import { Bricolage_Grotesque, Newsreader, JetBrains_Mono } from "next/font/google";
import "./globals.css";

// Três papéis, três famílias: display carrega a personalidade, corpo carrega a prosa,
// mono carrega metadado (id, rótulo, tag) — e nunca o contrário.
const bricolage = Bricolage_Grotesque({
  subsets: ["latin"],
  variable: "--font-bricolage",
  weight: ["400", "600", "800"],
});

const newsreader = Newsreader({
  subsets: ["latin"],
  variable: "--font-newsreader",
  weight: ["300", "400", "500"],
  style: ["normal", "italic"],
});

const jetbrainsMono = JetBrains_Mono({
  subsets: ["latin"],
  variable: "--font-jetbrains",
  weight: ["400", "500"],
});

export const metadata: Metadata = {
  title: "Pedro Chasci Puga — software engineer",
  description:
    "Software engineer building systems that have to keep running. Every project here is listed with the trade-off it cost.",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    // As variáveis do next/font ficam no <html>, não no <body>: os tokens de
    // globals.css são declarados em :root, e uma custom property é resolvida onde é
    // DECLARADA, não onde é usada. Com as fontes no <body>, `--font-body` referenciava
    // um `--font-newsreader` inexistente em :root, virava inválida, e a prosa caía no
    // sans padrão sem nenhum erro visível.
    <html
      lang="en"
      className={`${bricolage.variable} ${newsreader.variable} ${jetbrainsMono.variable}`}
    >
      <body>{children}</body>
    </html>
  );
}
