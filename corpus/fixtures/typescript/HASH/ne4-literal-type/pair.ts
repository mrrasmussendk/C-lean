declare function scale(value: number, by: number): number;
declare function zoom(value: number, by: number): number;
declare function report(...parts: unknown[]): void;
declare function emit(...parts: unknown[]): void;

export function left(items: number[], factor: number): number {
  let total = 0;
  const limit = 10;
  for (const item of items) {
    total += scale(item, factor);
  }
  while (total > limit) {
    total -= 1;
  }
  const label = "sum";
  report(label, total);
  return total + limit;
}

export function right(items: number[], factor: number): number {
  let total = 0;
  const limit = "10";
  for (const item of items) {
    total += scale(item, factor);
  }
  while (total > limit) {
    total -= 1;
  }
  const label = "sum";
  report(label, total);
  return total + limit;
}
