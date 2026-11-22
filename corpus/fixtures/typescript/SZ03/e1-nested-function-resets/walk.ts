export function walk(grid: number[][], n: number): number {
  let total = 0;
  for (const row of grid) {
    if (row.length > 0) {
      row.forEach((cell) => {
        for (let i = 0; i < cell; i++) {
          if (i > n) {
            total += 1;
          }
        }
      });
    }
  }
  return total;
}
