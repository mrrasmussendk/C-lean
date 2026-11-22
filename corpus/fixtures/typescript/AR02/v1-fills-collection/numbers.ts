export function collectEven(
  values: number[],
  found: number[],
): void {
  for (const value of values) {
    if (value % 2 === 0) {
      found.push(value);
    }
  }
}
