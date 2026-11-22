export function score(a: boolean, b: boolean, c: boolean, n: number): number {
  let total = 0;
  if (a && b && c && n > 0) {
    total += 1;
  } else if (a || b || c) {
    total += 2;
  } else if (n === 2 || n === 3 || n === 4) {
    total += 3;
  } else if (a && b && n === 5) {
    total += 4;
  } else if (b || c || n === 6) {
    total += 5;
  } else if (a && c && n === 7) {
    total += 6;
  } else {
    total += 7;
  }
  if (a && b) total += 8;
  return total;
}
