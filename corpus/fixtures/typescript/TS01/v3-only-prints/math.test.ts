import { expect, it } from "vitest";
import { add, parse } from "./math";

it("adds", () => {
  const result = add(1, 2);
  console.log(result, parse("3"));
});
