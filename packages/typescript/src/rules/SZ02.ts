// SZ02: Cognitive complexity of a function must not exceed the limit
import { cognitiveComplexity } from "../analysis/complexity";
import { onSymbol, productionFunctions } from "../analysis/report";
import { limitOf } from "../analysis/settings";
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";

const ADVICE = "Flatten nested conditions with early returns or move one decision into a function of its own";

const DEFAULT_LIMIT = 15;

export function SZ02(project: Project): Finding[] {
  const limit = limitOf(project.config, "SZ02", DEFAULT_LIMIT);
  return productionFunctions(project)
    .map((fn) => ({ fn, complexity: cognitiveComplexity(fn.node, project.checker) }))
    .filter((measured) => measured.complexity > limit)
    .map((measured) =>
      onSymbol("SZ02", measured.fn,
        `function ${measured.fn.name} has cognitive complexity ${measured.complexity}, limit ${limit}. ${ADVICE}`),
    );
}
