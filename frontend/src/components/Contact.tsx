import SectionHeader from "./SectionHeader";

const links = [
  { label: "GitHub", href: "https://github.com/PedroCPdev" },
  { label: "LinkedIn", href: "https://www.linkedin.com/in/pedrocpdev/" },
  { label: "Instagram", href: "https://www.instagram.com/pedrocpdev/" },
];

export default function Contact() {
  return (
    <section id="contact" style={{
      padding: "56px 40px",
      borderTop: "0.5px solid rgba(91,160,245,0.08)",
    }}>
      <SectionHeader number="03" title="Contact" />
      <div style={{
        display: "flex", alignItems: "center", justifyContent: "space-between",
        background: "#0d1b2e", border: "0.5px solid rgba(91,160,245,0.1)",
        borderRadius: "12px", padding: "28px 32px",
      }}>
        <div>
          <h3 style={{ fontSize: "16px", fontWeight: 500, color: "#e8f0fe", margin: "0 0 4px" }}>
            Vamos conversar?
          </h3>
          <p style={{ fontSize: "13px", color: "rgba(232,240,254,0.35)", margin: 0 }}>
            Aberto a oportunidades e colaborações.
          </p>
        </div>
        <div style={{ display: "flex", gap: "10px" }}>
          {links.map(({ label, href }) => (
            <a key={label} href={href} target="_blank" rel="noopener noreferrer" style={{
              display: "inline-flex", alignItems: "center", gap: "7px",
              padding: "9px 18px", borderRadius: "8px", fontSize: "13px",
              textDecoration: "none", border: "0.5px solid rgba(91,160,245,0.14)",
              background: "#050d1a", color: "rgba(232,240,254,0.55)", transition: "all 0.2s",
            }}>
              {label}
            </a>
          ))}
        </div>
      </div>
    </section>
  );
}