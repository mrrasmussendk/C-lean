import { type Order, countdown, describe, settle, total } from "../lib/calc.ts";

function quote(text: string): string {
  return `"${text}"`;
}

export async function main(): Promise<string[]> {
  const orders: Order[] = [
    { price: 10, quantity: 3, note: "gift" },
    { price: 2, quantity: 2 },
    { price: 200, quantity: 0, note: "" },
  ];
  const lines: string[] = [];
  for (const order of orders) {
    lines.push(`${total(order)} ${describe(order, (text) => quote(text))} ${await settle(order)}`);
  }
  const steps = countdown(3);
  let step = steps.next();
  let guard = 0;
  while (!step.done && guard < 10) {
    lines.push(`tick ${step.value}`);
    step = steps.next();
    guard += 1;
  }
  lines.push(`end ${step.done ? step.value : "running"}`);
  return lines;
}
