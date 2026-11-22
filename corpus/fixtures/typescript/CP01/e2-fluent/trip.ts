export class Query {
  private parts: string[] = [];

  withTable(name: string): Query {
    this.parts = [...this.parts, name];
    return this;
  }

  withFilter(filter: string): Query {
    this.parts = [...this.parts, filter];
    return this;
  }

  withOrder(order: string): Query {
    this.parts = [...this.parts, order];
    return this;
  }

  build(): string {
    return this.parts.join(" ");
  }
}

export function query(builder: Query): string {
  return builder.withTable("orders").withFilter("open").withOrder("date").build();
}
