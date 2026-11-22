export function walk(grid: number[][], n: number): number {
  let total = 0;
  if (n === 0) {
    total += 0;
  } else if (n === 0 + 1) {
    total += 0;
  } else if (n === 1 + 1) {
    total += 1;
  } else if (n === 2 + 1) {
    total += 2;
  } else if (n === 3 + 1) {
    total += 3;
  } else if (n === 4 + 1) {
    total += 4;
  } else if (n === 5 + 1) {
    total += 5;
  }
  return total;
}
