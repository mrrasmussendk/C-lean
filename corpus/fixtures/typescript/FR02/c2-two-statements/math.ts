function sum(a: number, b: number): number {
  return a + b;
}

export function add(a: number, b: number): number {
  if (a < 0) throw new Error("a must not be negative");
  return sum(a, b);
}
