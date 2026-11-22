// SZ04: A function must not declare more parameters than the limit
import { onSymbol, productionFunctions } from "../analysis/report";
import { limitOf } from "../analysis/settings";
import { isThisParameter } from "../analysis/syntax";
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";

const DEFAULT_LIMIT = 4;

export function SZ04(project: Project): Finding[] {
  const limit = limitOf(project.config, "SZ04", DEFAULT_LIMIT);
  return productionFunctions(project)
    .map((fn) => ({ fn, count: fn.node.parameters.filter((parameter) => !isThisParameter(parameter)).length }))
    .filter((measured) => measured.count > limit)
    .map((measured) =>
      onSymbol("SZ04", measured.fn, `function ${measured.fn.name} declares ${measured.count} parameters, limit ${limit}. Group parameters that travel together into one object, or split the function`),
    );
}
