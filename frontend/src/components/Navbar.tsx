"use client";
export default function Navbar() {
  return (
    <nav style={{
      display: "flex",
      justifyContent: "space-between",
      alignItems: "center",
      padding: "18px 40px",
      borderBottom: "0.5px solid rgba(91,160,245,0.12)",
      position: "sticky",
      top: 0,
      background: "#050d1a",
      zIndex: 100,
    }}>
      <span style={{
        fontFamily: "var(--font-mono)",
        fontSize: "14px",
        color: "#5ba0f5",
      }}>
        PedroCPDev
      </span>
      <ul style={{ display: "flex", gap: "28px", listStyle: "none", margin: 0, padding: 0 }}>
        {["about", "projects", "contact"].map((item) => (
          <li key={item}>
            <a href={`#${item}`} style={{
              fontSize: "13px",
              color: "rgba(232,240,254,0.45)",
              textDecoration: "none",
              letterSpacing: "0.03em",
              transition: "color 0.2s",
            }}
            onMouseEnter={e => (e.currentTarget.style.color = "#5ba0f5")}
            onMouseLeave={e => (e.currentTarget.style.color = "rgba(232,240,254,0.45)")}
            >
              {item}
            </a>
          </li>
        ))}
      </ul>
    </nav>
  );
}