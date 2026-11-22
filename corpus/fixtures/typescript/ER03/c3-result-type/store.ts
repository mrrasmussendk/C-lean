import { writeFileSync } from "node:fs";

export interface SaveResult {
  saved: boolean;
  reason: string;
}

export function save(path: string, text: string): SaveResult {
  if (text.length === 0) {
    return { saved: false, reason: "nothing to save" };
  }
  writeFileSync(path, text);
  return { saved: true, reason: "" };
}
