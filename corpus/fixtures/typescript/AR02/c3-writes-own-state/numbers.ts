export class Range {
  low = 0;
  high = 0;

  measure(values: number[]): void {
    this.low = Math.min(...values);
    this.high = Math.max(...values);
  }
}
