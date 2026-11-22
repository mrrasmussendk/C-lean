export function allPositive(values: readonly number[]): boolean {
  return values.every((value) => value > 0);
}
