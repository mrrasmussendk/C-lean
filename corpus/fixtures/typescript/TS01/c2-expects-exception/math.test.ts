import { expect, it } from "vitest";
import { add, parse } from "./math";

it("rejects empty text", () => {
  expect(() => parse("")).toThrow();
  add(1, 2);
});
