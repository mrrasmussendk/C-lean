// DS02: The same group of parameters must not travel through more functions than the limit
import * as ts from "typescript";
import { onSymbol, productionFunctions } from "../analysis/report";
import { limitOf } from "../analysis/settings";
import { inFileOrder, listed } from "../analysis/spread";
import { isThisParameter } from "../analysis/syntax";
import { declarationsAt } from "../analysis/types";
import type { Finding } from "../model/finding";
import type { FunctionNode, FunctionUnit, Project } from "../model/project";

const DEFAULT_LIMIT = 2;
const GROUP_SIZE = 3;
const ADVICE = "Make the group a type and pass that";

/** A function with its parameters as `name: type` keys, and the signature it repeats by inheritance, if any. */
interface Carrier {
  fn: FunctionUnit;
  keys: ReadonlySet<string>;
  family: ts.Node;
}

interface Clump {
  group: string[];
  carriers: Carrier[];
}

export function DS02(project: Project): Finding[] {
  const limit = limitOf(project.config, "DS02", DEFAULT_LIMIT);
  const carriers = productionFunctions(project)
    .map((fn) => ({ fn, keys: new Set(parameterKeys(fn.node)), family: familyOf(fn.node, project.checker) }))
    .filter((carrier) => carrier.keys.size >= GROUP_SIZE);
  const clumps = candidateGroups(carriers)
    .map((group) => ({ group, carriers: carriers.filter((carrier) => group.every((key) => carrier.keys.has(key))) }))
    .filter((clump) => new Set(clump.carriers.map((carrier) => carrier.family)).size > limit);
  return clumps.filter((clump) => !clumps.some((other) => isLarger(other, clump))).map((clump) => {
    const functions = inFileOrder(clump.carriers.map((carrier) => carrier.fn));
    const count = `parameters ${clump.group.join(", ")} travel together through ${functions.length} functions`;
    return onSymbol("DS02", functions[0], `${count} (${listed(functions)}), limit ${limit}. ${ADVICE}`);
  });
}

/** Same name and same declared type; an undeclared type matches only an undeclared type. The receiver is left out. */
function parameterKeys(fn: FunctionNode): string[] {
  return fn.parameters
    .filter((parameter) => !isThisParameter(parameter))
    .map((parameter) => {
      const declared = parameter.type?.getText().replace(/\s+/g, "") ?? "(undeclared)";
      return `${parameter.name.getText()}: ${declared}`;
    });
}

/** Every set of three or more parameters that two functions share, or that one function declares; each once. */
function candidateGroups(carriers: readonly Carrier[]): string[][] {
  const shared = carriers.flatMap((left) =>
    carriers.map((right) => [...left.keys].filter((key) => right.keys.has(key)).sort()),
  );
  const groups = shared.filter((group) => group.length >= GROUP_SIZE);
  const distinct = new Map(groups.map((group) => [group.join("|"), group]));
  return [...distinct.values()];
}

/** Whether `other` is a strictly larger group that holds every parameter of `clump`. */
function isLarger(other: Clump, clump: Clump): boolean {
  return other.group.length > clump.group.length && clump.group.every((key) => other.group.includes(key));
}

/** A method that overrides or implements another counts once with it: the topmost declaration of that member. */
function familyOf(fn: ts.Node, checker: ts.TypeChecker): ts.Node {
  const owner = fn.parent;
  const name = ts.isMethodDeclaration(fn) ? fn.name.getText() : "";
  if (!name || !ts.isClassLike(owner)) return fn;
  const inherited = (owner.heritageClauses ?? [])
    .flatMap((clause) => clause.types.flatMap((type) => declarationsAt(checker, type.expression)))
    .flatMap((base) => (ts.isClassLike(base) || ts.isInterfaceDeclaration(base) ? [...base.members] : []))
    .find((member) => member.name?.getText() === name);
  return inherited ? familyOf(inherited, checker) : fn;
}
