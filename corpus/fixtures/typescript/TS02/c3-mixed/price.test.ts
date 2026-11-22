import { expect, it, vi } from "vitest";
import { priceWithTax } from "./price";

it("doubles the loaded price", () => {
  const load = vi.fn().mockReturnValue(5);
  expect(load()).toBe(5);
  expect(priceWithTax(load)).toBe(10);
});
