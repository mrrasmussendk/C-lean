// SZ05: Type SLOC must not exceed the limit
import { onSymbol, productionTypes } from "../analysis/report";
import { limitOf } from "../analysis/settings";
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";

const ADVICE = "Move one responsibility with its data into a type of its own";

const DEFAULT_LIMIT = 200;

export function SZ05(project: Project): Finding[] {
  const limit = limitOf(project.config, "SZ05", DEFAULT_LIMIT);
  return productionTypes(project)
    .filter((type) => type.sloc > limit)
    .map((type) =>
      onSymbol("SZ05", type, `type ${type.name} has ${type.sloc} SLOC, limit ${limit}. ${ADVICE}`),
    );
}
