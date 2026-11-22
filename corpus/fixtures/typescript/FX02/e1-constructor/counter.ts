export class Counter {
  private count: number;

  constructor(start: number) {
    this.count = start;
  }

  current(): number {
    return this.count;
  }
}
