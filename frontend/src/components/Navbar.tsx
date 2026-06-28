"use client";
export default function Navbar() {
  return (
    <nav className="flex justify-between items-center py-[22px] px-4 sm:px-8 md:px-12 border-b-[0.5px] border-[#5ba0f5]/12 sticky top-0 bg-[#050d1a] z-50 w-full">
      <span className="font-mono text-sm text-[#5ba0f5]">
        PedroCPDev
      </span>
      <ul className="flex items-center list-none m-0 p-0 gap-1">
        {["about", "projects", "contact"].map((item) => (
          <li key={item}>
            <a
              href={`#${item}`}
              className="block text-[12px] sm:text-[13px] text-[#e8f0fe]/45 no-underline tracking-[0.03em] transition-colors duration-200 hover:text-[#5ba0f5] px-2.5 sm:px-3.5 py-1"
            >
              {item}
            </a>
          </li>
        ))}
      </ul>
    </nav>
  );
}