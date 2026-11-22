export class Cart {
  private items: number[] = [];
  private coupon = "";

  total(): number {
    return this.items.length;
  }
}
