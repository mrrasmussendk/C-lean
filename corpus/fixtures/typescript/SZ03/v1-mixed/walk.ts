export function walk(grid: number[][], n: number): number {
  let total = 0;
  while (total < n) {
    if (n > 0) {
      for (const row of grid) {
        if (row.length > 0) {
          total += row.length;
        }
      }
    }
    total += 1;
  }
  return total;
}
