import { expect, it, vi } from "vitest";
import { priceWithTax } from "./price";

it("asks for the price twice", () => {
  const load = vi.fn().mockReturnValue(5);
  priceWithTax(load);
  expect(load).toHaveBeenCalledTimes(2);
});
