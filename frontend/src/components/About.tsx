import SectionHeader from "./SectionHeader";
import { SiDotnet, SiSharp, SiPostgresql, SiReact, SiDocker, SiAngular, SiGit } from "react-icons/si";
import { IconType } from "react-icons";

const tagIcons: Record<string, IconType> = {
  "C#": SiSharp,
  ".NET": SiDotnet,
  "AngularJS": SiAngular,
  "PostgreSQL": SiPostgresql,
  "Git": SiGit,
  "Docker": SiDocker,
  "React": SiReact,
};

const tags = ["C#", ".NET", "Oracle", "EF Core", "AngularJS", "PostgreSQL", "Git", "Docker", "Azure", "Agile", "Scrum", "TDD", "Clean Code", "DDD", "Microservices", "CI/CD", "Unit Testing", "Integration Testing", "Performance Optimization"];

export default function About() {
  return (
    <section id="about" className="py-12 md:py-20 px-4 sm:px-8 md:px-12 border-t-[0.5px] border-[#5ba0f5]/8 w-full">
      <SectionHeader number="01" title="About me" />
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 md:gap-8 w-full">
        <div className="bg-[#0d1b2e] border-[0.5px] border-[#5ba0f5]/10 rounded-[10px] py-6 px-6 flex flex-col gap-3.5">
          <p className="text-[11px] text-[#e8f0fe]/30 uppercase tracking-[0.08em] mb-2.5">
            Current main role
          </p>
          <p className="text-sm text-[#c8d8f0] leading-relaxed m-0">
            <span className="font-mono text-[13px] text-[#5ba0f5]">.NET</span> Software Developer
            <br />
            <span className="text-xs text-[#e8f0fe]/[0.28]">
              maintenance and development of .NET, Oracle and AngularJS applications
            </span>
          </p>
        </div>
        <div className="bg-[#0d1b2e] border-[0.5px] border-[#5ba0f5]/10 rounded-[10px] py-6 px-6 flex flex-col gap-3.5">
          <p className="text-[11px] text-[#e8f0fe]/30 uppercase tracking-[0.08em] mb-2.5">
            Main technologies
          </p>
          <div className="flex flex-wrap gap-2.5 mt-1.5">
            {tags.map((tag) => {
              const Icon = tagIcons[tag];
              return (
                <span
                  key={tag}
                  className="font-mono text-[10px] py-0.75 px-2 rounded bg-[#5ba0f5]/8 text-[#5ba0f5] border-[0.5px] border-[#5ba0f5]/18 inline-flex items-center gap-1"
                >
                  {Icon && <Icon size={10} />}
                  {tag}
                </span>
              );
            })}
          </div>
        </div>
      </div>
    </section>
  );
}