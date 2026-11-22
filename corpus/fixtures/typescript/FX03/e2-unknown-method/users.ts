export class Guests {
  private names: string[] = [];

  add(name: string): void {
    this.names = [...this.names, name];
  }

  count(): number {
    return this.names.length;
  }
}

export function invite(guests: Guests, name: string): number {
  guests.add(name);
  return guests.count();
}
