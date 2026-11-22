// SZ01: Function body SLOC must not exceed the limit
import { onSymbol, productionFunctions } from "../analysis/report";
import { limitOf } from "../analysis/settings";
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";

const DEFAULT_LIMIT = 30;

export function SZ01(project: Project): Finding[] {
  const limit = limitOf(project.config, "SZ01", DEFAULT_LIMIT);
  return productionFunctions(project)
    .filter((fn) => fn.sloc > limit)
    .map((fn) =>
      onSymbol("SZ01", fn, `function ${fn.name} has ${fn.sloc} SLOC, limit ${limit}. Split it at a point where the extracted part has a name that is not a restatement of its body`),
    );
}
