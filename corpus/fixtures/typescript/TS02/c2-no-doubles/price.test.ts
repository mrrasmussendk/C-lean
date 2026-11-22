import { expect, it, vi } from "vitest";
import { priceWithTax } from "./price";

it("doubles the loaded price", () => {
  expect(priceWithTax(() => 5)).toBe(10);
});
