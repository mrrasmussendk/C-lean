import { expect, it, vi } from "vitest";
import { priceWithTax } from "./price";

it("doubles the loaded price", () => {
  const load = vi.fn().mockReturnValue(5);
  const price = priceWithTax(load);
  expect(price).toBe(10);
});
