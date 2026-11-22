export class Counter {
  private readonly count = 0;

  doubled(): number {
    return this.count + this.count;
  }
}
