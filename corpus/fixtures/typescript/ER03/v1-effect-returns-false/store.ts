import { writeFileSync } from "node:fs";

export function save(path: string, text: string): boolean {
  if (text.length === 0) {
    return false;
  }
  writeFileSync(path, text);
  return true;
}
