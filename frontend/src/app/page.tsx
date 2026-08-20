import Navbar from "@/components/Navbar";
import Hero from "@/components/Hero";
import About from "@/components/About";
import Projects from "@/components/Projects";
import Contact from "@/components/Contact";

export default function Home() {
  return (
    <div id="top" className="flex min-h-screen flex-col">
      <Navbar />
      <main className="flex-1">
        <Hero />
        <About />
        <Projects />
        <Contact />
      </main>
      <footer className="border-t border-rule">
        <div className="shell flex flex-wrap items-center justify-between gap-2 py-8 font-mono text-[0.6875rem] text-ink-dim">
          <span>Pedro Chasci Puga — 2026</span>
          <span>Next.js frontend · ASP.NET Core API · Firestore</span>
        </div>
      </footer>
    </div>
  );
}
