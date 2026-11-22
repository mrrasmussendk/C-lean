export function score(a: boolean, b: boolean, c: boolean, n: number): number {
  let total = 0;
  if (a && b || c) {
    total += 1;
  } else if (b && c || a) {
    total += 2;
  } else if (a || b && c) {
    total += 3;
  } else if (c && a || b) {
    total += 4;
  } else {
    total += 5;
  }
  if (a && b || c) total += n;
  return total;
}
