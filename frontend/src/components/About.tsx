import SectionHeader from "./SectionHeader";

const tags = ["C#", ".NET", "Oracle", "EF Core", "AngularJS", "PostgreSQL"];

export default function About() {
  return (
    <section id="about" style={{
      padding: "56px 40px",
      borderTop: "0.5px solid rgba(91,160,245,0.08)",
    }}>
      <SectionHeader number="01" title="About me" />
      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "14px" }}>
        <div style={{
          background: "#0d1b2e", border: "0.5px solid rgba(91,160,245,0.1)",
          borderRadius: "10px", padding: "20px 22px",
        }}>
          <p style={{ fontSize: "11px", color: "rgba(232,240,254,0.3)", textTransform: "uppercase", letterSpacing: "0.08em", marginBottom: "8px" }}>
            ocupação atual
          </p>
          <p style={{ fontSize: "14px", color: "#c8d8f0", lineHeight: 1.6 }}>
            <span style={{ fontFamily: "var(--font-mono)", fontSize: "13px", color: "#5ba0f5" }}>.NET</span> Software Developer
            <br />
            <span style={{ fontSize: "12px", color: "rgba(232,240,254,0.28)" }}>
              manutenção e desenvolvimento de aplicações .NET, Oracle e AngularJS
            </span>
          </p>
        </div>
        <div style={{
          background: "#0d1b2e", border: "0.5px solid rgba(91,160,245,0.1)",
          borderRadius: "10px", padding: "20px 22px",
        }}>
          <p style={{ fontSize: "11px", color: "rgba(232,240,254,0.3)", textTransform: "uppercase", letterSpacing: "0.08em", marginBottom: "8px" }}>
            stack principal
          </p>
          <div style={{ display: "flex", flexWrap: "wrap", gap: "6px", marginTop: "6px" }}>
            {tags.map(tag => (
              <span key={tag} style={{
                fontFamily: "var(--font-mono)", fontSize: "10px", padding: "3px 8px",
                borderRadius: "4px", background: "rgba(91,160,245,0.08)",
                color: "#5ba0f5", border: "0.5px solid rgba(91,160,245,0.18)",
              }}>
                {tag}
              </span>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}