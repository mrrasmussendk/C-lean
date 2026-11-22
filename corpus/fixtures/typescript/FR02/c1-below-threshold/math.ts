function scale(a: number, factor: number): number {
  return a * factor;
}

export function grow(a: number, label: string): number {
  return scale(a, 1);
}
