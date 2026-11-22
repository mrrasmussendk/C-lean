export class Cart {
  private items: number[] = [];

  total(): number {
    return this.items.length;
  }

  private discount(): number {
    return 0;
  }
}
