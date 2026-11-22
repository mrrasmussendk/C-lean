import { label } from "../format/label.ts";

export class Items {
  private readonly names: string[];

  constructor(names: string[]) {
    this.names = [...names];
  }

  /** Calls back into the caller's code for every item. */
  each(visit: (name: string) => string): string[] {
    return this.names.map((name) => visit(name));
  }

  /** Suspends at every yield; the caller runs in its own component in between. */
  *walk(): Generator<string, number, string | undefined> {
    let sent = 0;
    for (const name of this.names) {
      const reply = yield name;
      if (reply !== undefined) sent += 1;
    }
    yield* this.tail();
    return sent;
  }

  private *tail(): Generator<string, void, string | undefined> {
    yield "end";
  }

  /** Awaits, then calls another component: the edge must be store to format. */
  async load(delay: number): Promise<string> {
    await new Promise((resolve) => setTimeout(resolve, delay));
    return label(this.names.join("+"));
  }

  async failing(): Promise<string> {
    await Promise.resolve();
    throw new Error("store failed");
  }

  first(): string {
    if (this.names.length === 0) throw new Error("no items");
    return this.names[0];
  }
}
