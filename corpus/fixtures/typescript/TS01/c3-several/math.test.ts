import { expect, it } from "vitest";
import { add, parse } from "./math";

it("adds", () => {
  expect(add(1, 2)).toBe(3);
  expect(add(0, 0)).toBe(0);
  expect(parse("3")).toBe(3);
});
