import { expect, it } from "vitest";
import { answer } from "./answer";

it("is not the answer", () => {
  expect(answer()).not.toBe(42);
});
