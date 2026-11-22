function sum(a: number, b: number): number {
  return a + b;
}

export function add(a: number, b: string): number {
  return sum(a + 1, b.trim().length);
}
