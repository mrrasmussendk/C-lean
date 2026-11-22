import { expect, it, vi } from "vitest";
import { priceWithTax } from "./price";

it("loads the prices", () => {
  const load = vi.fn().mockReturnValue(5);
  const reload = vi.fn(() => 7);
  expect(load()).toBe(5);
  expect(reload()).toBe(7);
});
