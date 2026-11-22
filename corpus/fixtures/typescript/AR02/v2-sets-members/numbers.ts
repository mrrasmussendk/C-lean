export interface Range {
  low: number;
  high: number;
}

export function measure(
  values: number[],
  range: Range,
): void {
  range.low = Math.min(...values);
  range.high = Math.max(...values);
}
