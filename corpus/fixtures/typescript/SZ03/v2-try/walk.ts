export function walk(grid: number[][], n: number): number {
  let total = 0;
  for (const row of grid) {
    if (row.length > 0) {
      try {
        if (row[0] > n) {
          total += row[0];
        }
      } finally {
        total += 1;
      }
    }
  }
  return total;
}
