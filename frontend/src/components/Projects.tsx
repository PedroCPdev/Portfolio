/* eslint-disable react/jsx-no-comment-textnodes */
import SectionHeader from "./SectionHeader";
import { getProjects, Project } from "@/lib/api";

function ProjectCard({ project }: { project: Project }) {
  return (
    <div style={{
      background: "#0d1b2e",
      border: "0.5px solid rgba(91,160,245,0.1)",
      borderRadius: "10px",
      padding: "20px 22px",
      display: "flex",
      flexDirection: "column",
      gap: "12px",
    }}>
      <div>
        <h3 style={{ fontSize: "14px", fontWeight: 500, color: "#e8f0fe", margin: "0 0 6px" }}>
          {project.title}
        </h3>
        <p style={{ fontSize: "12px", color: "rgba(232,240,254,0.4)", margin: 0, lineHeight: 1.6 }}>
          {project.description}
        </p>
      </div>

      <div style={{ display: "flex", flexWrap: "wrap", gap: "6px" }}>
        {project.tags.map(tag => (
          <span key={tag} style={{
            fontFamily: "var(--font-mono)",
            fontSize: "10px",
            padding: "3px 8px",
            borderRadius: "4px",
            background: "rgba(91,160,245,0.08)",
            color: "#5ba0f5",
            border: "0.5px solid rgba(91,160,245,0.18)",
          }}>
            {tag}
          </span>
        ))}
      </div>

      <div style={{ display: "flex", gap: "10px", marginTop: "auto" }}>
        {project.githubUrl && (
          <a
            href={project.githubUrl}
            target="_blank"
            rel="noopener noreferrer"
            style={{
              fontSize: "12px",
              color: "rgba(232,240,254,0.4)",
              textDecoration: "none",
              border: "0.5px solid rgba(91,160,245,0.14)",
              padding: "4px 12px",
              borderRadius: "6px",
              background: "#050d1a",
            }}
          >
            GitHub
          </a>
        )}
        {project.liveUrl && (
          <a
            href={project.liveUrl}
            target="_blank"
            rel="noopener noreferrer"
            style={{
              fontSize: "12px",
              color: "#5ba0f5",
              textDecoration: "none",
              border: "0.5px solid rgba(91,160,245,0.3)",
              padding: "4px 12px",
              borderRadius: "6px",
              background: "rgba(91,160,245,0.06)",
            }}
          >
            Live →
          </a>
        )}
      </div>
    </div>
  );
}

export default async function Projects() {
  const projects = await getProjects();

  return (
    <section id="projects" style={{
      padding: "56px 40px",
      borderTop: "0.5px solid rgba(91,160,245,0.08)",
    }}>
      <SectionHeader number="02" title="Projects" />

      {projects.length === 0 ? (
        <p style={{
          fontSize: "13px",
          color: "rgba(232,240,254,0.25)",
          fontFamily: "var(--font-mono)",
        }}>
          // nenhum projeto encontrado — verifique se a API está rodando
        </p>
      ) : (
        <div style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fill, minmax(280px, 1fr))",
          gap: "14px",
        }}>
          {projects.map(project => (
            <ProjectCard key={project.id} project={project} />
          ))}
        </div>
      )}
    </section>
  );
}