export function score(a: boolean, b: boolean, c: boolean, n: number): number {
  let total = 0;
  if (a) total += 1;
  for (let i = 0; i < n; i++) {
    if (a) {
      for (let j = 0; j < n; j++) {
        if (b) {
          while (total < n) {
            total += 1;
          }
        }
      }
    }
  }
  return total;
}
