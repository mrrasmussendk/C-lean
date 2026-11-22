const PIN_ROWS = [
  [7, 8, 9, 10],
  [4, 5, 6],
  [2, 3],
  [1],
] as const;

export function rowOf(pin: number): number {
  return PIN_ROWS.findIndex((row) => row.some((candidate) => candidate === pin));
}
