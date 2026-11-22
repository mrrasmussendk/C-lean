import { writeFileSync } from "node:fs";

export function save(path: string, text: string): void {
  if (text.length === 0) {
    throw new Error("nothing to save");
  }
  writeFileSync(path, text);
}
