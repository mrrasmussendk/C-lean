export function priceWithTax(load: () => number): number {
  return load() + load();
}
