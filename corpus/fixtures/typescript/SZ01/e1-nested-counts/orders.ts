export function outer(values: number[]): number {
  let total = 0;
  total += values.length + 0;
  total += values.length + 1;
  total += values.length + 2;
  total += values.length + 3;
  total += values.length + 4;
  total += values.length + 5;
  total += values.length + 6;
  total += values.length + 7;
  total += values.length + 8;
  total += values.length + 9;
  total += values.length + 10;
  total += values.length + 11;
  total += values.length + 12;
  total += values.length + 13;
  total += values.length + 14;
  total += values.length + 15;
  function inner(): number {
    let total = 0;
    total += values.length + 0;
    total += values.length + 1;
    total += values.length + 2;
    total += values.length + 3;
    total += values.length + 4;
    total += values.length + 5;
    total += values.length + 6;
    total += values.length + 7;
    total += values.length + 8;
    total += values.length + 9;
    total += values.length + 10;
    total += values.length + 11;
    total += values.length + 12;
    return total;
  }
  return total + inner();
}
