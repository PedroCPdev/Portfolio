import Section from "./Section";
import Ledger from "./Ledger";
import { getProjects, Project } from "@/lib/api";
import { FaGithub } from "react-icons/fa";
import { FiExternalLink } from "react-icons/fi";

// O trilho carrega o ano do registro, nao um ordinal decorativo: a ordem da lista nao
// significa nada para quem le, a data significa (DR-06).
function ProjectCard({ project }: { project: Project }) {
  const year = new Date(project.createdAt).getUTCFullYear();
  return (
    <article className="border-t border-rule py-10 first:border-t-0 first:pt-0">
      <div className="min-w-0">
        <p className="label m-0 mb-3 text-amber">
          {Number.isNaN(year) ? "\u2014" : year}
        </p>
        <h3 className="m-0 font-display text-[1.375rem] font-semibold tracking-[-0.015em] text-ink">
          {project.title}
        </h3>

        <div className="mt-5">
          <Ledger kept={project.description} cost={project.tradeoff} />
        </div>

        <ul className="m-0 mt-6 flex list-none flex-wrap gap-x-4 gap-y-2 p-0">
          {project.tags.map((tag) => (
            <li key={tag} className="font-mono text-[0.75rem] text-ink-dim">
              {tag}
            </li>
          ))}
        </ul>

        {(project.githubUrl || project.liveUrl) && (
          <div className="mt-6 flex flex-wrap items-center gap-5">
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
    </article>
  );
}

export default async function Projects() {
  const projects = await getProjects();

  return (
    <Section id="work" rail={<span>Work</span>} title="Selected records">
      {projects.length === 0 ? (
        <p className="m-0 max-w-[48ch] text-ink-dim">
          The project list is served by the API behind this page, and it is not answering
          right now. It runs on a free tier that sleeps — give it a minute and reload, or
          read the source on{" "}
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
        <div className="flex flex-col">
          {projects.map((project) => (
            <ProjectCard key={project.id} project={project} />
          ))}
        </div>
      )}
    </Section>
  );
}
