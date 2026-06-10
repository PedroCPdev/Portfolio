/* eslint-disable react/jsx-no-comment-textnodes */
export default function Hero() {
  return (
    <section style={{ padding: "80px 40px 72px", display: "flex", flexDirection: "column", gap: "14px" }}>
      <span style={{ fontFamily: "var(--font-mono)", fontSize: "12px", color: "#5ba0f5", letterSpacing: "0.08em" }}>
        // hello world
      </span>
      <h1 style={{ fontSize: "44px", fontWeight: 600, color: "#e8f0fe", lineHeight: 1.1, margin: 0 }}>
        Pedro <span style={{ color: "#5ba0f5" }}>Chasci</span> Puga
      </h1>
      <p style={{ fontSize: "16px", color: "rgba(232,240,254,0.45)", fontWeight: 300, margin: 0 }}>
        Software Developer
      </p>
      <p style={{ fontSize: "14px", color: "rgba(232,240,254,0.3)", fontStyle: "italic" }}>
        Passionate developer
      </p>
      <div style={{ display: "flex", gap: "12px", marginTop: "12px" }}>
        <a href="#projects" style={{
          display: "inline-flex", alignItems: "center", gap: "6px",
          padding: "9px 20px", borderRadius: "8px", fontSize: "13px",
          textDecoration: "none", border: "0.5px solid #1a3a6b",
          background: "#0d1b2e", color: "#e8f0fe", transition: "all 0.2s",
        }}>
          ver projetos
        </a>
        <a href="#contact" style={{
          display: "inline-flex", alignItems: "center", gap: "6px",
          padding: "9px 20px", borderRadius: "8px", fontSize: "13px",
          textDecoration: "none", border: "0.5px solid rgba(91,160,245,0.3)",
          background: "rgba(91,160,245,0.06)", color: "#5ba0f5", transition: "all 0.2s",
        }}>
          contato
        </a>
      </div>
    </section>
  );
}