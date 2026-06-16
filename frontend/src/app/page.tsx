import Navbar from "@/components/Navbar";
import Hero from "@/components/Hero";
import About from "@/components/About";
import Projects from "@/components/Projects";
import Contact from "@/components/Contact";

export default function Home() {
  return (
    <div className="max-w-5xl mx-auto w-full flex flex-col min-h-screen">
      <Navbar />
      <main className="flex-1">
        <Hero />
        <About />
        <Projects />
        <Contact />
      </main>
      <footer className="py-8 px-10 border-t-[0.5px] border-[#5ba0f5]/8 text-center text-[11px] text-[#e8f0fe]/15 font-mono">
        Pedro Chasci Puga © 2026 — built with next.js + c#
      </footer>
    </div>
  );
}