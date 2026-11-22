export function walk(grid: number[][], n: number): number {
  let total = 0;
  if (n > 0) {
    for (const row of grid) {
      if (row.length > 0) {
        total += row.length;
      }
    }
  }
  return total;
}
