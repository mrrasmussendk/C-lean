import { expect, it } from "vitest";
import { add, parse } from "./math";

function expectSum(a: number, b: number, sum: number): void {
  expect(add(a, b)).toBe(sum);
}

it("adds", () => {
  expectSum(1, 2, parse("3"));
});
