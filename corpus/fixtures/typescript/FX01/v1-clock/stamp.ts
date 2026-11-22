export function stamp(label: string): string {
  const now = Date.now();
  return `${label}@${now}`;
}
