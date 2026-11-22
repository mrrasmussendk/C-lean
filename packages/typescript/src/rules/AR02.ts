// AR02: Output parameter: a function must not return data by writing through a parameter. Exempt: the language's standard try-parse pattern
import * as ts from "typescript";
import { parameterWrites } from "../analysis/mutation";
import { onParameter, productionFunctions } from "../analysis/report";
import { everyFlag, returnedType } from "../analysis/types";
import type { Finding } from "../model/finding";
import type { FunctionNode, Project } from "../model/project";

const NOTHING_OR_BOOLEAN = ts.TypeFlags.Void | ts.TypeFlags.Undefined | ts.TypeFlags.BooleanLike;

export function AR02(project: Project): Finding[] {
  return productionFunctions(project)
    .filter((fn) => returnsNothingOrBoolean(project.checker, fn.node))
    .flatMap((fn) =>
      writtenParameters(project.checker, fn.node).map((parameter) =>
        onParameter("AR02", fn, parameter, `function ${fn.name} returns data by writing into its parameter ${parameter.name.getText()}. Return the data as the function's result`),
      ),
    );
}

/** TypeScript has no out or ref parameters and no try-parse pattern, so only the return type decides. */
function returnsNothingOrBoolean(checker: ts.TypeChecker, fn: FunctionNode): boolean {
  const returned = returnedType(checker, fn);
  return returned !== undefined && everyFlag(returned, NOTHING_OR_BOOLEAN);
}

function writtenParameters(checker: ts.TypeChecker, fn: FunctionNode): ts.ParameterDeclaration[] {
  return [...new Set(parameterWrites(checker, fn).map((write) => write.parameter))];
}
