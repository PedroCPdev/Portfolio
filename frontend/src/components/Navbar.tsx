const items = [
  { href: "#about", label: "About" },
  { href: "#work", label: "Work" },
  { href: "#contact", label: "Contact" },
];

export default function Navbar() {
  return (
    <nav className="sticky top-0 z-50 border-b border-rule bg-ground/95 backdrop-blur-sm">
      <div className="mx-auto flex max-w-5xl items-center justify-between px-6 py-4 md:px-10">
        <a
          href="#top"
          className="font-mono text-[0.8125rem] tracking-[0.02em] text-ink no-underline"
        >
          pedrocpdev
        </a>
        <ul className="m-0 flex list-none items-center gap-1 p-0">
          {items.map(({ href, label }) => (
            <li key={href}>
              <a
                href={href}
                className="block px-3 py-1.5 font-mono text-[0.75rem] text-ink-dim no-underline transition-colors hover:text-amber"
              >
                {label}
              </a>
            </li>
          ))}
        </ul>
      </div>
    </nav>
  );
}
