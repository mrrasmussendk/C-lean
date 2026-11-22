export interface Store {
  save(value: string): void;
}

export class MemoryStore implements Store {
  values: string[] = [];

  save(value: string): void {
    this.values = [...this.values, value];
  }
}
