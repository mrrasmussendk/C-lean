// SZ03: Nesting depth of control structures in a function must not exceed the limit
import { nestingDepth } from "../analysis/complexity";
import { onSymbol, productionFunctions } from "../analysis/report";
import { limitOf } from "../analysis/settings";
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";

const DEFAULT_LIMIT = 3;

export function SZ03(project: Project): Finding[] {
  const limit = limitOf(project.config, "SZ03", DEFAULT_LIMIT);
  return productionFunctions(project)
    .map((fn) => ({ fn, depth: nestingDepth(fn.node.body) }))
    .filter((measured) => measured.depth > limit)
    .map((measured) =>
      onSymbol("SZ03", measured.fn, `function ${measured.fn.name} nests control structures ${measured.depth} deep, limit ${limit}. Return early or move the inner structure into a function of its own`),
    );
}
