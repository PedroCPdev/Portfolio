/* eslint-disable react/jsx-no-comment-textnodes */
import SectionHeader from "./SectionHeader";
import { getProjects, Project } from "@/lib/api";
import { FaGithub } from "react-icons/fa";
import { FiExternalLink } from "react-icons/fi";

function ProjectCard({ project }: { project: Project }) {
  return (
    <div className="bg-[#0d1b2e] border-[0.5px] border-[#5ba0f5]/10 rounded-[10px] py-7 px-6 sm:px-8 flex flex-col gap-5">
      <div>
        <h3 className="text-sm font-medium text-[#e8f0fe] m-0 mb-1.5">
          {project.title}
        </h3>
        <p className="text-xs text-[#e8f0fe]/40 m-0 leading-relaxed">
          {project.description}
        </p>
      </div>
      <div className="flex flex-wrap gap-2.5">
        {project.tags.map((tag) => (
          <span
            key={tag}
            className="font-mono text-[10px] py-0.75 px-2 rounded bg-[#5ba0f5]/8 text-[#5ba0f5] border-[0.5px] border-[#5ba0f5]/18"
          >
            {tag}
          </span>
        ))}
      </div>
      <div className="flex items-center gap-3.5 mt-auto">
        {project.githubUrl && (
          <a
            href={project.githubUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-1.5 text-xs text-[#e8f0fe]/40 no-underline border-[0.5px] border-[#5ba0f5]/[0.14] py-1 px-3 rounded-md bg-[#050d1a] transition-colors hover:text-[#e8f0fe]"
          >
            <FaGithub size={12} />
            GitHub
          </a>
        )}
        {project.liveUrl && (
          <a
            href={project.liveUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-1.5 text-xs text-[#5ba0f5] no-underline border-[0.5px] border-[#5ba0f5]/30 py-1 px-3 rounded-md bg-[#5ba0f5]/6 transition-colors hover:bg-[#5ba0f5]/20"
          >
            <FiExternalLink size={12} />
            Live
          </a>
        )}
      </div>
    </div>
  );
}

export default async function Projects() {
  const projects = await getProjects();
  return (
    <section id="projects" className="py-12 md:py-20 px-4 sm:px-8 md:px-12 border-t-[0.5px] border-[#5ba0f5]/8 w-full">
      <SectionHeader number="02" title="Projects" />
      {projects.length === 0 ? (
        <p className="text-[13px] text-[#e8f0fe]/25 font-mono m-0 ml-1">
          // no projects yet, but stay tuned! I&apos;m working on some exciting projects that will be showcased here soon.
        </p>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-6 md:gap-8 w-full">
          {projects.map((project) => (
            <ProjectCard key={project.id} project={project} />
          ))}
        </div>
      )}
    </section>
  );
}