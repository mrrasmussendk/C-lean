import { readFileSync } from "node:fs";

export function print(text: string): void {
  console.log(text);
}

export function readBytes(path: string): Buffer {
  return readFileSync(path);
}
