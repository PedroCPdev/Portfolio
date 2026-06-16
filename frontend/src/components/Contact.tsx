import SectionHeader from "./SectionHeader";
import { FaGithub, FaLinkedin, FaInstagram, FaEnvelope } from "react-icons/fa";

const links = [
  { label: "GitHub",    href: "https://github.com/PedroCPdev",          icon: FaGithub    },
  { label: "LinkedIn",  href: "https://www.linkedin.com/in/pedrocpdev/", icon: FaLinkedin  },
  { label: "Instagram", href: "https://www.instagram.com/pedrocpdev/",   icon: FaInstagram },
  { label: "Email",     href: "mailto:pedrocpdev@gmail.com",               icon: FaEnvelope  },
];

export default function Contact() {
  return (
    <section id="contact" className="py-20 px-12 border-t-[0.5px] border-[#5ba0f5]/8 w-full">
      <SectionHeader number="03" title="Contact" />
      <div className="flex items-center justify-between bg-[#0d1b2e] border-[0.5px] border-[#5ba0f5]/10 rounded-xl py-10 px-12">
        <div>
          <h3 className="text-base font-medium text-[#e8f0fe] m-0 mb-2">
            let&apos;s talk?
          </h3>
          <p className="text-[13px] text-[#e8f0fe]/35 m-0">
            Open to opportunities and collaborations.
          </p>
        </div>
        <div className="flex items-center gap-4">
          {links.map(({ label, href, icon: Icon }) => (
            <a
              key={label}
              href={href}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-1.75 py-2.5 px-5 rounded-lg text-[13px] no-underline border-[0.5px] border-[#5ba0f5]/[0.14] bg-[#050d1a] text-[#e8f0fe]/[0.55] transition-all duration-200 hover:text-[#5ba0f5] hover:border-[#5ba0f5]/30"
            >
              <Icon size={16} />
              {label}
            </a>
          ))}
        </div>
      </div>
    </section>
  );
}