import { expect, it } from "vitest";
import { add, parse } from "./math";

it("adds", () => {
  add(1, 2);
  parse("3");
});
