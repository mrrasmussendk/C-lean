// Sample for the switch check: every operator kind, laziness, a generator, an async function and a callback.
export interface Order {
  price: number;
  quantity: number;
  note?: string;
}

export function total(order: Order): number {
  const gross = order.price * order.quantity;
  const fee = gross / 4 - 1;
  return gross + fee;
}

export function describe(order: Order, visit: (text: string) => string): string {
  const big = order.quantity >= 3 || order.price > 100;
  const cheap = order.price < 5 && order.quantity <= 2;
  const noted = order.note !== undefined && order.note.length != 0;
  const plain = !noted;
  const exact = order.price === 10 ? true : false;
  const same = order.quantity == order.price;
  return visit([big, cheap, noted, plain, exact, same].join(","));
}

export function* countdown(from: number): Generator<number, string, undefined> {
  let current = from;
  while (current > 0) {
    yield current * 2;
    current = current - 1;
  }
  return current === 0 ? "liftoff" : "overshot";
}

export async function settle(order: Order): Promise<number> {
  const before = total(order);
  await Promise.resolve();
  return before - order.quantity;
}

function neverCalled(order: Order): boolean {
  return order.price + order.quantity > 0 && !order.note;
}

export const spare = [neverCalled].length;
