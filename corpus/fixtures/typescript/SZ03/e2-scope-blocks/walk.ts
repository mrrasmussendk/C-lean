export function walk(grid: number[][], n: number): number {
  let total = 0;
  {
    const rows = grid;
    {
      const limit = n;
      if (limit > 0) {
        for (const row of rows) {
          if (row.length > limit) {
            total += row.length;
          }
        }
      }
    }
  }
  return total;
}
