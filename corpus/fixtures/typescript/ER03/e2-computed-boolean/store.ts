export class Stack {
  private items: string[] = [];

  push(item: string): boolean {
    const before = this.items.length;
    this.items = [...this.items, item].filter((kept) => kept.length > 0);
    return this.items.length > before;
  }
}
