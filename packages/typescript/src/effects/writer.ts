/** Writes the files a command produced. The only module that writes to the file system. */
import * as fs from "node:fs";
import * as path from "node:path";
import { failure } from "../model/failure";
import type { OutputFile } from "../model/inputs";

export function writeFiles(files: readonly OutputFile[]): void {
  for (const file of files) {
    try {
      fs.mkdirSync(path.dirname(file.path), { recursive: true });
      fs.writeFileSync(file.path, file.text);
    } catch (error) {
      throw failure(file.path, `the file cannot be written (${error instanceof Error ? error.message : String(error)})`, "Check that the directory exists and is writable");
    }
  }
}
