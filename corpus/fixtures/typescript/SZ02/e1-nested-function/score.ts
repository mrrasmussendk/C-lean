export function score(values: number[], n: number): number {
  let total = 0;
  if (n === 0) total += 0;
  if (n === 1) total += 1;
  if (n === 2) total += 2;
  if (n === 3) total += 3;
  if (n === 4) total += 4;
  if (n === 5) total += 5;
  if (n === 6) total += 6;
  if (n === 7) total += 7;
  values.forEach((value) => {
    if (value === 0) total += 0;
    if (value === 1) total += 1;
    if (value === 2) total += 2;
    if (value === 3) total += 3;
  });
  return total;
}
