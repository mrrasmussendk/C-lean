export class Cart {
  private items: number[] = [];

  total(): number {
    return this.items.length - this.discount();
  }

  private discount(): number {
    return 0;
  }
}
