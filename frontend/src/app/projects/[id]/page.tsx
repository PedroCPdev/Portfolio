import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import Navbar from "@/components/Navbar";
import DecisionRecord from "@/components/DecisionRecord";
import { getProject, getProjects } from "@/lib/api";
import { FaGithub } from "react-icons/fa";
import { FiExternalLink } from "react-icons/fi";

type Params = { params: Promise<{ id: string }> };

// Pré-renderiza os projetos conhecidos no build. `getProjects` engole falha e devolve [],
// então a API fora do ar não quebra o build — os ids caem para renderização sob demanda.
export async function generateStaticParams() {
  const projects = await getProjects();
  return projects.map((project) => ({ id: project.id }));
}

export async function generateMetadata({ params }: Params): Promise<Metadata> {
  const { id } = await params;
  const project = await getProject(id);
  if (!project) return { title: "Project not found — Pedro Chasci Puga" };

  return {
    title: `${project.title} — Pedro Chasci Puga`,
    description: project.description,
  };
}

export default async function ProjectPage({ params }: Params) {
  const { id } = await params;
  const project = await getProject(id);

  if (!project) notFound();

  const decisions = project.decisions ?? [];
  const year = new Date(project.createdAt).getUTCFullYear();

  return (
    <div className="flex min-h-screen flex-col">
      <Navbar />

      <main className="flex-1">
        <header className="border-t border-rule">
          <div className="shell grid gap-x-10 gap-y-6 py-14 md:py-20 lg:grid-cols-[var(--spine)_1fr]">
            <div className="label flex flex-row gap-4 pt-2 lg:flex-col lg:gap-1.5 lg:text-right">
              <span className="text-amber">{Number.isNaN(year) ? "—" : year}</span>
              <span>Record</span>
            </div>

            <div className="min-w-0">
              <Link
                href="/#work"
                className="inline-flex items-center gap-2 font-mono text-[0.75rem] text-ink-dim no-underline transition-colors hover:text-amber"
              >
                <span aria-hidden>&larr;</span> Back to work
              </Link>

              <h1 className="m-0 mt-6 font-display text-[clamp(2rem,5vw,3.25rem)] font-extrabold leading-[1.02] tracking-[-0.03em] text-ink">
                {project.title}
              </h1>

              <p className="measure mt-6 text-[1.0625rem] leading-relaxed text-ink-dim">
                {project.description}
              </p>

              <ul className="m-0 mt-7 flex list-none flex-wrap gap-x-4 gap-y-2 p-0">
                {project.tags.map((tag) => (
                  <li key={tag} className="font-mono text-[0.75rem] text-ink-dim">
                    {tag}
                  </li>
                ))}
              </ul>

              {(project.githubUrl || project.liveUrl) && (
                <div className="mt-7 flex flex-wrap items-center gap-5">
                  {project.githubUrl && (
                    <a
                      href={project.githubUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="inline-flex items-center gap-2 font-mono text-[0.8125rem] text-ink-dim no-underline transition-colors hover:text-ink"
                    >
                      <FaGithub size={13} aria-hidden />
                      Source
                    </a>
                  )}
                  {project.liveUrl && (
                    <a
                      href={project.liveUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="inline-flex items-center gap-2 font-mono text-[0.8125rem] text-amber no-underline transition-colors hover:text-ink"
                    >
                      <FiExternalLink size={13} aria-hidden />
                      Live
                    </a>
                  )}
                </div>
              )}
            </div>
          </div>
        </header>

        <section className="border-t border-rule">
          <div className="shell grid gap-x-10 gap-y-5 py-14 md:py-20 lg:grid-cols-[var(--spine)_1fr]">
            <div className="label pt-2 lg:text-right">
              <span>Decisions</span>
            </div>

            <div className="min-w-0">
              {decisions.length === 0 ? (
                // PD-07: sem decisões a seção diz o que existe, em vez de mostrar bloco vazio.
                <p className="measure m-0 text-ink-dim">
                  The decisions behind this one are not written up yet. The source is the
                  honest version in the meantime.
                </p>
              ) : (
                <div className="flex flex-col">
                  {decisions.map((decision, i) => (
                    <DecisionRecord key={decision.title} decision={decision} index={i} />
                  ))}
                </div>
              )}
            </div>
          </div>
        </section>
      </main>

      <footer className="border-t border-rule">
        <div className="shell flex flex-wrap items-center justify-between gap-2 py-8 font-mono text-[0.6875rem] text-ink-dim">
          <span>Pedro Chasci Puga — 2026</span>
          <Link href="/" className="text-ink-dim no-underline transition-colors hover:text-amber">
            Back to the start
          </Link>
        </div>
      </footer>
    </div>
  );
}
