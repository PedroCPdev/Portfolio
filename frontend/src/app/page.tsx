import Navbar from "@/components/Navbar";
import Hero from "@/components/Hero";
import About from "@/components/About";
import Contact from "@/components/Contact";

export default function Home() {
  return (
    <>
      <Navbar />
      <main>
        <Hero />
        <About />
        <Contact />
      </main>
      <footer style={{
        padding: "20px 40px",
        borderTop: "0.5px solid rgba(91,160,245,0.08)",
        textAlign: "center",
        fontSize: "11px",
        color: "rgba(232,240,254,0.15)",
        fontFamily: "var(--font-mono)",
      }}>
        pedro chasci puga © 2026 — built with next.js + c#
      </footer>
    </>
  );
}