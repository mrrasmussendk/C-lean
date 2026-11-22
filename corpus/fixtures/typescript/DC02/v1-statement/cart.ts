export function total(prices: number[]): number {
  let sum = 0;
  // sum = sum + prices.length;
  for (const price of prices) sum += price;
  return sum;
}
