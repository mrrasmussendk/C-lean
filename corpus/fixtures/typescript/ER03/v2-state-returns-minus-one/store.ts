export class Stack {
  private items: string[] = [];
  private pushes = 0;

  push(item: string): number {
    if (item.length === 0) {
      return -1;
    }
    this.items = [...this.items, item];
    this.pushes += 1;
    return this.items.length + this.pushes;
  }
}
