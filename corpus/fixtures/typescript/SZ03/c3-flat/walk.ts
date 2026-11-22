export function walk(grid: number[][], n: number): number {
  let total = 0;
  if (n === 0) {
    total += 0;
  }
  for (const row of grid) {
    total += row.length + 0;
  }
  if (n === 1) {
    total += 1;
  }
  for (const row of grid) {
    total += row.length + 1;
  }
  if (n === 2) {
    total += 2;
  }
  for (const row of grid) {
    total += row.length + 2;
  }
  if (n === 3) {
    total += 3;
  }
  for (const row of grid) {
    total += row.length + 3;
  }
  if (n === 4) {
    total += 4;
  }
  for (const row of grid) {
    total += row.length + 4;
  }
  return total;
}
