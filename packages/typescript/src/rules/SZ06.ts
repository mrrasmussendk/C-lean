// SZ06: A source line must not exceed the limit in characters
import { onLine, productionFiles } from "../analysis/report";
import { limitOf } from "../analysis/settings";
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";

const DEFAULT_LIMIT = 120;

export function SZ06(project: Project): Finding[] {
  const limit = limitOf(project.config, "SZ06", DEFAULT_LIMIT);
  return productionFiles(project).flatMap((unit) =>
    unit.source.text
      .split("\n")
      .map((line, index) => ({ number: index + 1, length: [...line.replace(/\r$/, "")].length }))
      .filter((line) => line.length > limit)
      .map((line) => onLine("SZ06", unit, line.number, `line ${line.number} is ${line.length} characters long, limit ${limit}. Break it at an operator or a call, or name a part of the expression`)),
  );
}
