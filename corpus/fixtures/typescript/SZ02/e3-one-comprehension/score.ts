export function score(cells: number[], n: number): number[] {
  const picked: number[] = [];
  for (const cell of cells) {
    if (cell > n) picked.push(cell);
  }
  return picked;
}
