import { shout } from "../format/label.ts";
import { Items } from "../store/items.ts";

function decorate(name: string): string {
  return `<${name}>`;
}

export async function main(): Promise<string[]> {
  const items = new Items(["a", "b"]);
  const lines: string[] = [];
  lines.push(...items.each((name) => decorate(name)));

  const walker = items.walk();
  let step = walker.next();
  while (!step.done) {
    lines.push(shout(step.value));
    step = walker.next("ok");
  }
  lines.push(`sent ${step.value}`);

  const pending = items.load(1);
  lines.push(shout("while loading"));
  lines.push(await pending);

  try {
    await items.failing();
  } catch (error) {
    lines.push(`caught ${(error as Error).message}`);
  }
  try {
    new Items([]).first();
  } catch (error) {
    lines.push(`caught ${(error as Error).message}`);
  }
  lines.push(shout("done"));
  return lines;
}
