// FX03: A function must not mutate an object it received as an argument
import { parameterWrites } from "../analysis/mutation";
import { onNode, productionFunctions } from "../analysis/report";
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";

export function FX03(project: Project): Finding[] {
  return productionFunctions(project).flatMap((fn) =>
    parameterWrites(project.checker, fn.node).map((write) =>
      onNode("FX03", fn.path, write.site, `function ${fn.name} mutates its argument ${write.parameter.name.getText()}. Build and return a new value, or make the mutated object the receiver`),
    ),
  );
}
