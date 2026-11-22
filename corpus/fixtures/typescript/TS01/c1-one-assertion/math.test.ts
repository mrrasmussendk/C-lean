import { expect, it } from "vitest";
import { add, parse } from "./math";

it("adds", () => {
  expect(add(1, 2)).toBe(Number(parse("3")));
});
