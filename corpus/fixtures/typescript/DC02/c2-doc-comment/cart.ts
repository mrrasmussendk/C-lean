/**
 * Counts the entries of `prices` and returns the count as `total`.
 * The caller passes the result on to `render`.
 */
export function total(prices: number[]): number {
  return prices.length;
}
