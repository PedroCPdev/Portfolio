import Section from "./Section";
import { SiSharp, SiDotnet, SiPostgresql, SiDocker, SiFirebase, SiAngular } from "react-icons/si";
import { IconType } from "react-icons";

// DR-11 — hierarquia em vez de nuvem plana. O núcleo é o que se defende numa entrevista;
// o resto é contexto e vem com peso visual menor, não igual.
const core: { name: string; icon?: IconType }[] = [
  { name: "C#", icon: SiSharp },
  { name: ".NET", icon: SiDotnet },
  { name: "ASP.NET Core" },
  { name: "PostgreSQL", icon: SiPostgresql },
  { name: "Firestore", icon: SiFirebase },
  { name: "Docker", icon: SiDocker },
];

const alsoUsed: { name: string; icon?: IconType }[] = [
  { name: "Oracle" },
  { name: "EF Core" },
  { name: "AngularJS", icon: SiAngular },
  { name: "Azure" },
  { name: "CI/CD" },
];

export default function About() {
  return (
    <Section id="about" rail={<span>Profile</span>} title="How I work">
      <div className="flex flex-col gap-10">
        <p className="measure m-0 text-ink-dim">
          I maintain and build .NET services — the kind that sit behind something else and
          are only noticed when they stop. Most of what I do is deciding which constraint to
          accept: a free tier that sleeps, a database without joins, a seed that fails without
          taking the process down with it. I write those decisions down, including the ones
          that aged badly.
        </p>

        <div>
          <p className="label m-0 mb-4">Core</p>
          <ul className="m-0 flex list-none flex-wrap gap-2 p-0">
            {core.map(({ name, icon: Icon }) => (
              <li
                key={name}
                className="inline-flex items-center gap-2 rounded-sm border border-amber/25 bg-amber/[0.07] px-3 py-1.5 font-mono text-[0.8125rem] text-amber"
              >
                {Icon && <Icon size={13} aria-hidden />}
                {name}
              </li>
            ))}
          </ul>
        </div>

        <div>
          <p className="label m-0 mb-4">Also worked with</p>
          <ul className="m-0 flex list-none flex-wrap gap-x-4 gap-y-2 p-0">
            {alsoUsed.map(({ name, icon: Icon }) => (
              <li
                key={name}
                className="inline-flex items-center gap-1.5 font-mono text-[0.75rem] text-ink-dim"
              >
                {Icon && <Icon size={11} aria-hidden />}
                {name}
              </li>
            ))}
          </ul>
        </div>
      </div>
    </Section>
  );
}
