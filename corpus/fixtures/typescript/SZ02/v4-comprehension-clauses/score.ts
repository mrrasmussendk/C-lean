export function score(grid: number[][], n: number): number {
  let total = 0;
  if (n === 0) total += 0;
  if (n === 1) total += 1;
  if (n === 2) total += 2;
  if (n === 3) total += 3;
  if (n === 4) total += 4;
  if (n === 5) total += 5;
  if (n === 6) total += 6;
  if (n === 7) total += 7;
  if (n === 8) total += 8;
  if (n === 9) total += 9;
  if (n === 10) total += 10;
  if (n === 11) total += 11;
  const picked = (() => {
    const found: number[] = [];
    for (const row of grid) {
      if (row.length > n) {
        for (const cell of row) {
          if (cell > n) found.push(cell);
        }
      }
    }
    return found;
  })();
  return total + picked.length;
}
