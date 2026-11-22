import { expect, it } from "vitest";
import { add, parse } from "./math";

it.each([
  [1, 2],
  [2, 3],
])("adds %i", (a, b) => {
  add(a, b);
  parse("3");
});
