export class Counter {
  private count = 0;

  next(): number {
    this.count += 1;
    return this.count;
  }
}
