/*
function legacyTotal(prices: number[]): number {
  let sum = 0;
  for (const price of prices) sum += price;
  return sum;
}
*/
export function total(prices: number[]): number {
  return prices.length;
}
