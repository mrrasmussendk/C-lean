// Sample for the mutants check. The non-ASCII text below makes byte offsets differ from character offsets.
const RATE = 2 + 3;
const ENABLED = true;

export enum Level {
  Low = 1 + 1,
}

declare const logger: { warn(...parts: unknown[]): void };

export function label(name: string, count: number): string {
  const greeting = "héllo wörld — ça va " + name;
  return greeting + "×" + count;
}

export function price(amount: number, quantity: number, discount: number): number {
  const café = amount * quantity - discount / RATE;
  if (!ENABLED || quantity <= 0) {
    throw new Error("ünusable");
  }
  if (amount < 0 && discount >= 0) {
    logger.warn("négatif", amount + quantity, amount === discount);
    return café + 1;
  }
  const flags: Array<true | false> = [true, false];
  const spaced = amount+-quantity;
  const inverted = typeof!flags === "boolean";
  return flags.length > 1 && amount != quantity && !inverted ? café : spaced;
}

export class Counter {
  static readonly START = 1 + 1;
  private step = 1 + 1;

  next(value: number): number {
    return value !== Counter.START ? value + this.step : value * this.step;
  }
}
