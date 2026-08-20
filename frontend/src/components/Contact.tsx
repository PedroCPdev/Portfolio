import Section from "./Section";
import ContactForm from "./ContactForm";
import { FaGithub, FaLinkedin, FaEnvelope } from "react-icons/fa";

const links = [
  { label: "GitHub", href: "https://github.com/PedroCPdev", icon: FaGithub },
  { label: "LinkedIn", href: "https://www.linkedin.com/in/pedrocpdev/", icon: FaLinkedin },
  { label: "Email", href: "mailto:pedrocpdev@gmail.com", icon: FaEnvelope },
];

export default function Contact() {
  return (
    <Section id="contact" rail={<span>Contact</span>} title="Start a conversation">
      <div className="flex flex-col gap-10">
        <p className="measure m-0 text-ink-dim">
          Open to engineering roles in any language or stack. Tell me what you are building
          and what constraint you are stuck on.
        </p>

        <ContactForm />

        <div className="flex flex-col gap-4 border-t border-rule pt-8">
          <p className="label m-0">Or reach me directly</p>
          <ul className="m-0 flex list-none flex-wrap gap-x-6 gap-y-3 p-0">
            {links.map(({ label, href, icon: Icon }) => (
              <li key={label}>
                <a
                  href={href}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-2 font-mono text-[0.8125rem] text-ink-dim no-underline transition-colors hover:text-amber"
                >
                  <Icon size={14} aria-hidden />
                  {label}
                </a>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </Section>
  );
}
