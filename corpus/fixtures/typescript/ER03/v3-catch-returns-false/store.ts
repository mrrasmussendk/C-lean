import { writeFileSync } from "node:fs";

export function save(path: string, text: string): boolean {
  try {
    writeFileSync(path, text);
  } catch (error) {
    return false;
  }
  return true;
}
