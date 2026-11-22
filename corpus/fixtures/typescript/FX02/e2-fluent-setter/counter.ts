export class Counter {
  private step = 1;

  withStep(step: number): Counter {
    this.step = step;
    return this;
  }

  current(): number {
    return this.step;
  }
}
