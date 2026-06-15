interface Props {
  number: string;
  title: string;
}

export default function SectionHeader({ number, title }: Props) {
  return (
    <div className="flex items-center gap-4 mb-10 w-full">
      <span className="font-mono text-[11px] text-[#5ba0f5]/40 m-0">
        {number}
      </span>
      <h2 className="text-xl font-medium text-[#e8f0fe] m-0">
        {title}
      </h2>
      <div className="flex-1 h-[0.5px] bg-[#5ba0f5]/10" />
    </div>
  );
}