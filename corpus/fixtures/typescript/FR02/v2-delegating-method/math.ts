export class Index {
  find(table: string, key: string, limit: number): string {
    return `${table}/${key}/${limit}`;
  }
}

export class Catalog {
  private readonly index = new Index();

  lookup(table: string, key: string, limit: number): string {
    return this.index.find(table, key, limit);
  }
}
