export class Guests {
  private names: string[] = [];

  add(name: string): void {
    this.names.push(name);
  }

  count(): number {
    return this.names.length;
  }
}
