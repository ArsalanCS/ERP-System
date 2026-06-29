import type { SVGProps } from 'react';

/**
 * Inline SVG icon registry. Lucide-style: 24×24 viewBox, fill:none,
 * stroke:currentColor, stroke-width 1.7, round caps/joins (per design handoff).
 * Swap for a real icon library later keeping the same stroke weight.
 */
export type IconName =
  | 'home'
  | 'gauge'
  | 'users'
  | 'shield'
  | 'sitemap'
  | 'lock'
  | 'history'
  | 'settings'
  | 'user'
  | 'search'
  | 'plus'
  | 'bell'
  | 'help'
  | 'chevron-down'
  | 'chevron-right'
  | 'log-out'
  | 'languages'
  | 'bolt'
  | 'close'
  | 'edit'
  | 'trash'
  | 'check'
  | 'more'
  | 'filter'
  | 'download'
  | 'refresh'
  | 'alert'
  | 'mail'
  | 'key'
  | 'building'
  | 'chevron-left'
  | 'arrow-right'
  | 'chart'
  | 'calendar'
  | 'link';

const PATHS: Record<IconName, string> = {
  home: 'M3 10.5 12 3l9 7.5M5 9.5V21h14V9.5',
  gauge: 'M12 14a2 2 0 1 0 0-4 2 2 0 0 0 0 4Zm0 0 4.5-4.5M5 19a9 9 0 1 1 14 0',
  users: 'M16 19v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M9 9a3 3 0 1 0 0-6 3 3 0 0 0 0 6Zm13 10v-2a4 4 0 0 0-3-3.87M16 3.13A4 4 0 0 1 16 11',
  shield: 'M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z',
  sitemap: 'M9 3h6v4H9zM3 17h6v4H3zm12 0h6v4h-6zM12 7v4m0 0H6v3m6-3h6v3',
  lock: 'M5 11h14v10H5zM8 11V7a4 4 0 0 1 8 0v4',
  history: 'M3 12a9 9 0 1 0 3-6.7L3 8m0-5v5h5m4-1v5l3 2',
  settings:
    'M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6Zm7.4-3a7.4 7.4 0 0 0-.1-1.2l2-1.6-2-3.4-2.4 1a7.3 7.3 0 0 0-2-1.2l-.4-2.6h-4l-.4 2.6a7.3 7.3 0 0 0-2 1.2l-2.4-1-2 3.4 2 1.6a7.4 7.4 0 0 0 0 2.4l-2 1.6 2 3.4 2.4-1a7.3 7.3 0 0 0 2 1.2l.4 2.6h4l.4-2.6a7.3 7.3 0 0 0 2-1.2l2.4 1 2-3.4-2-1.6c.07-.4.1-.8.1-1.2Z',
  user: 'M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8Z',
  search: 'M21 21l-4.3-4.3M11 19a8 8 0 1 0 0-16 8 8 0 0 0 0 16Z',
  plus: 'M12 5v14M5 12h14',
  bell: 'M18 8a6 6 0 1 0-12 0c0 7-3 9-3 9h18s-3-2-3-9M13.7 21a2 2 0 0 1-3.4 0',
  help: 'M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20Zm-2-12a2 2 0 1 1 3 1.7c-.8.5-1 1-1 1.8M12 17h.01',
  'chevron-down': 'M6 9l6 6 6-6',
  'chevron-right': 'M9 6l6 6-6 6',
  'log-out': 'M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4m7 14 5-5-5-5m5 5H9',
  languages: 'M5 8h10M9 4v4m1.5 0c0 4-3 8-7.5 9M6 11c0 3 2.5 5.5 6 6.5M14 21l4-9 4 9m-7-2h6',
  bolt: 'M13 2 4 14h7l-1 8 9-12h-7l1-8Z',
  close: 'M18 6 6 18M6 6l12 12',
  edit: 'M12 20h9M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z',
  trash: 'M3 6h18M8 6V4h8v2m-9 0v14h10V6M10 11v5m4-5v5',
  check: 'M20 6 9 17l-5-5',
  more: 'M12 13a1 1 0 1 0 0-2 1 1 0 0 0 0 2Zm0-7a1 1 0 1 0 0-2 1 1 0 0 0 0 2Zm0 14a1 1 0 1 0 0-2 1 1 0 0 0 0 2Z',
  filter: 'M3 5h18l-7 8v6l-4 2v-8L3 5Z',
  download: 'M12 3v12m0 0 4-4m-4 4-4-4M4 21h16',
  refresh: 'M21 12a9 9 0 1 1-3-6.7L21 8m0-5v5h-5',
  alert: 'M12 9v4m0 4h.01M10.3 3.9 2 18a2 2 0 0 0 1.7 3h16.6a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z',
  mail: 'M4 5h16v14H4zM4 6l8 6 8-6',
  key: 'M14 7a4 4 0 1 1-4 4l-7 7v3h3l1-1v-2h2v-2h2l1-1',
  building: 'M3 21h18M5 21V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v16M9 7h2m-2 4h2m4-4h2m-2 4h2m-7 10v-4h6v4',
  'chevron-left': 'M15 6l-6 6 6 6',
  'arrow-right': 'M5 12h14m-6-6 6 6-6 6',
  chart: 'M4 20V4m0 16h16M8 16v-4m4 4V8m4 8v-6',
  calendar: 'M4 6h16v15H4zM4 9h16M8 3v4m8-4v4',
  link: 'M10 13a5 5 0 0 0 7 0l3-3a5 5 0 0 0-7-7l-1 1m1 6a5 5 0 0 0-7 0l-3 3a5 5 0 0 0 7 7l1-1',
};

interface IconProps extends Omit<SVGProps<SVGSVGElement>, 'name'> {
  name: IconName;
  size?: number;
}

export function Icon({ name, size = 18, ...props }: IconProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.7}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      {...props}
    >
      <path d={PATHS[name]} />
    </svg>
  );
}
