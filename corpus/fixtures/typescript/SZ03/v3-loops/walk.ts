export function walk(grid: number[][], n: number): number {
  let total = 0;
  for (const row of grid) {
    for (const cell of row) {
      for (let i = 0; i < cell; i++) {
        for (let j = 0; j < n; j++) {
          total += 1;
        }
      }
    }
  }
  return total;
}
