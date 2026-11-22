function blend(a: number, b: number, c: number, d: number, weight: number): number {
  return (a + b + c + d) * weight;
}

export function mix(a: number, b: number, c: number, d: number, label: string): number {
  return blend(a, b, c, d, 1);
}
