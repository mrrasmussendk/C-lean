export function add(a: number, b: number): number {
  return a + b;
}

export function parse(text: string): number {
  if (text.length === 0) throw new Error("empty");
  return Number(text);
}
