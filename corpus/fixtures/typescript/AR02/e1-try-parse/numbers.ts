export function parseNumber(text: string): number | undefined {
  const value = Number(text);
  return Number.isNaN(value) ? undefined : value;
}
