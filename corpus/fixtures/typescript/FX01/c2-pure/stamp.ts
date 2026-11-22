export function stamp(label: string, count: number): string {
  return `${label.trim().toUpperCase()}@${count + 1}`;
}
