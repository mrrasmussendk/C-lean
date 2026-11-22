import { readFileSync } from "node:fs";

export function load(path: string): string {
  const text = readFileSync(path, "utf8");
  return text.trim();
}
