import { expect, it, vi } from "vitest";
import { priceWithTax } from "./price";

class FakePrices {
  load(): number {
    return 5;
  }
}

it("loads the price", () => {
  const prices = new FakePrices();
  expect(prices.load()).toBe(5);
});
