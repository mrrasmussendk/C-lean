export interface Parsed {
  value: number;
}

export function readNumber(
  text: string,
  parsed: Parsed,
): boolean {
  const value = Number(text);
  if (Number.isNaN(value)) {
    return false;
  }
  parsed.value = value;
  return true;
}
