/* eslint-disable react/jsx-no-comment-textnodes */
export default function Hero() {
  return (
    <section className="pt-20 md:pt-24 px-4 sm:px-8 md:px-12 pb-16 md:pb-20 flex flex-col gap-6 w-full">
      <span className="font-mono text-xs text-[#5ba0f5] tracking-[0.08em]">
        // hello world
      </span>
      <h1 className="text-[32px] sm:text-[38px] md:text-[44px] font-semibold text-[#e8f0fe] leading-[1.1] m-0">
        Pedro <span className="text-[#5ba0f5]">Chasci</span> Puga
      </h1>
      <p className="text-base text-[#e8f0fe]/45 font-light m-0">
        Software Developer
      </p>
      <p className="text-sm text-[#e8f0fe]/30 italic m-0">
        Focused on writing clean, maintainable code — from .NET APIs to cloud-deployed solutions.
      </p>
      <div className="flex items-center gap-3 mt-6 flex-wrap">
        <a
          href="#projects"
          className="inline-flex items-center gap-1.5 py-2.5 px-6 rounded-lg text-[13px] no-underline border-[0.5px] border-[#1a3a6b] bg-[#0d1b2e] text-[#e8f0fe] transition-all duration-200 hover:bg-[#1a3a6b]/50"
        >
          my projects
        </a>
        <a
          href="#contact"
          className="inline-flex items-center gap-1.5 py-2.5 px-6 rounded-lg text-[13px] no-underline border-[0.5px] border-[#5ba0f5]/30 bg-[#5ba0f5]/6 text-[#5ba0f5] transition-all duration-200 hover:bg-[#5ba0f5]/10"
        >
          contact me
        </a>
      </div>
    </section>
  );
}