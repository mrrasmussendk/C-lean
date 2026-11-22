export class Counter {
  private count = 0;

  advance(): void {
    this.count += 1;
  }

  current(): number {
    return this.count;
  }
}
