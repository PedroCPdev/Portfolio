interface Props {
  number: string;
  title: string;
}

export default function SectionHeader({ number, title }: Props) {
  return (
    <div style={{ display: "flex", alignItems: "center", gap: "16px", marginBottom: "32px" }}>
      <span style={{ fontFamily: "var(--font-mono)", fontSize: "11px", color: "rgba(91,160,245,0.4)" }}>
        {number}
      </span>
      <h2 style={{ fontSize: "20px", fontWeight: 500, color: "#e8f0fe", margin: 0 }}>
        {title}
      </h2>
      <div style={{ flex: 1, height: "0.5px", background: "rgba(91,160,245,0.1)" }} />
    </div>
  );
}