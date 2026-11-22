function double(price: number): number {
  return price + price;
}

export function doubled(prices: number[]): number[] {
  return prices.map(double);
}
