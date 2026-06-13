type ClassValue = string | number | false | null | undefined;

/**
 * Tiny className combiner. Filters out falsy values and joins with a space.
 * Kept dependency-free; swap for clsx/tailwind-merge if conflict resolution
 * becomes necessary as the component library grows.
 */
export function cn(...values: ClassValue[]): string {
  return values.filter(Boolean).join(' ');
}
