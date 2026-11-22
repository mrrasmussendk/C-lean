export function step(values: number[], index: number): number {
  if (index === -1) return 0;
  return values[0] + values[index + 1] - 1;
}
