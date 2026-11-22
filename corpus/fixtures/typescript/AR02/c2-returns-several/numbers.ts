export function range(values: number[]): [number, number] {
  return [Math.min(...values), Math.max(...values)];
}
