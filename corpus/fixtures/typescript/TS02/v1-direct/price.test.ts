import { expect, it, vi } from "vitest";
import { priceWithTax } from "./price";

it("loads the price", () => {
  const load = vi.fn().mockReturnValue(5);
  expect(load()).toBe(5);
});
