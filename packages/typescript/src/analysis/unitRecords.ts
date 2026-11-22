/** Builds the records of units.json: one per function with a body. */
import * as ts from "typescript";
import type { FunctionUnit, Project } from "../model/project";
import type { Graph } from "./graph";
import { normalizedHash } from "./hash";
import { limitOf } from "./settings";
import { endLine, ownDescendants, startLine } from "./syntax";

const PRODUCTION_WINDOW = 6;
const TEST_WINDOW = 4;

interface Sequence {
  hash: string;
  startLine: number;
  endLine: number;
}

export interface UnitRecord {
  id: string;
  file: string;
  startLine: number;
  endLine: number;
  sloc: number;
  hash: string;
  test: boolean;
  callers: number;
  sequences: Sequence[];
}

export function unitRecords(project: Project, graph: Graph): UnitRecord[] {
  const functions = project.files.flatMap((unit) => unit.functions);
  const production = new Set(functions.filter((fn) => !fn.test).map((fn) => fn.id));
  const calls = graph.edges.filter((edge) => edge.kind === "call" && production.has(edge.from));
  const windows =
    { production: limitOf(project.config, "DU01", PRODUCTION_WINDOW),
      test: limitOf(project.config, "TS03", TEST_WINDOW) };
  return functions.map((fn) => ({
    id: fn.id,
    file: fn.path,
    startLine: startLine(fn.node),
    endLine: endLine(fn.node),
    sloc: fn.sloc,
    hash: normalizedHash([fn.node.body]),
    test: fn.test,
    callers: calls.filter((edge) => edge.to === fn.id).length,
    sequences: sequencesOf(fn, fn.test ? windows.test : windows.production),
  }));
}

/** Every window of `size` consecutive statements in each block of the body, nested functions excluded. */
function sequencesOf(fn: FunctionUnit, size: number): Sequence[] {
  return [fn.node.body, ...ownDescendants(fn.node.body)].flatMap(statementsOf).flatMap((statements) =>
    statements.slice(0, Math.max(0, statements.length - size + 1)).map((first, index) => {
      const window = statements.slice(index, index + size);
      return { hash: normalizedHash(window), startLine: startLine(first), endLine: endLine(window[window.length - 1]) };
    }),
  );
}

function statementsOf(node: ts.Node): ts.Statement[][] {
  const isBlock = ts.isBlock(node) || ts.isCaseClause(node) || ts.isDefaultClause(node);
  return isBlock ? [[...node.statements]] : [];
}
