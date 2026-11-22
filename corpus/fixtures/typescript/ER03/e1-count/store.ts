import { writeFileSync } from "node:fs";

export function saveAll(path: string, texts: string[]): number {
  let saved = 0;
  for (const text of texts) {
    writeFileSync(`${path}.${saved}`, text);
    saved += 1;
  }
  return saved;
}
