import Link from "next/link";
import Section from "./Section";
import { getProjects, Project } from "@/lib/api";

// A home é vitrine: título, resumo, tags e o caminho para o detalhe. O raciocínio de
// engenharia mora em /projects/{id} — misturar os dois era o que deixava a página pesada.
function ProjectCard({ project }: { project: Project }) {
  const decisionCount = project.decisions?.length ?? 0;

  return (
    <li className="min-w-0">
      <Link
        href={`/projects/${project.id}`}
        className="group flex h-full flex-col gap-5 rounded-sm border border-rule bg-paper/40 p-6 no-underline transition-colors hover:border-amber/40 md:p-7"
      >
        <div className="min-w-0">
          <h3 className="m-0 font-display text-[1.25rem] font-semibold leading-tight tracking-[-0.015em] text-ink">
            {project.title}
          </h3>
          <p className="m-0 mt-3 text-[0.9375rem] leading-relaxed text-ink-dim">
            {project.description}
          </p>
        </div>

        <ul className="m-0 mt-auto flex list-none flex-wrap gap-x-3 gap-y-1.5 p-0">
          {project.tags.map((tag) => (
            <li key={tag} className="font-mono text-[0.6875rem] text-ink-dim">
              {tag}
            </li>
          ))}
        </ul>

        <p className="m-0 flex items-center gap-2 font-mono text-[0.75rem] text-amber">
          {decisionCount > 0
            ? `${decisionCount} ${decisionCount === 1 ? "decision" : "decisions"}`
            : "Read more"}
          <span aria-hidden className="transition-transform group-hover:translate-x-1">
            &rarr;
          </span>
        </p>
      </Link>
    </li>
  );
}

export default async function Projects() {
  const projects = await getProjects();

  return (
    <Section id="work" rail={<span>Work</span>} title="Selected work">
      {projects.length === 0 ? (
        <p className="measure m-0 text-ink-dim">
          The project list is served by the API behind this page, and it is not answering right
          now. It runs on a free tier that sleeps — give it a minute and reload, or read the
          source on{" "}
          <a
            href="https://github.com/PedroCPdev"
            target="_blank"
            rel="noopener noreferrer"
            className="text-amber underline decoration-amber/40 underline-offset-4"
          >
            GitHub
          </a>{" "}
          in the meantime.
        </p>
      ) : (
        <ul className="m-0 grid list-none grid-cols-1 gap-5 p-0 sm:grid-cols-2 xl:grid-cols-3">
          {projects.map((project) => (
            <ProjectCard key={project.id} project={project} />
          ))}
        </ul>
      )}
    </Section>
  );
}
