/** Renders a status/priority name as a pill tinted by its configured color. */
export function TaskStatusBadge({ name, color }: { name: string | null; color?: string | null }) {
  if (!name) return <span className="text-ink-4">—</span>;
  const c = color || '#64748b';
  return (
    <span
      className="inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 text-[12px] font-medium"
      style={{ color: c, backgroundColor: `${c}1a` }}
    >
      <span className="h-1.5 w-1.5 rounded-full" style={{ backgroundColor: c }} />
      {name}
    </span>
  );
}
