// FR02: Pass-through: a function whose body is one call that forwards at least 80% of its parameters
import * as ts from "typescript";
import { onSymbol, productionFiles } from "../analysis/report";
import { limitOf } from "../analysis/settings";
import { ancestors, awaited, isThisParameter } from "../analysis/syntax";
import type { Finding } from "../model/finding";
import type { FunctionNode, FunctionUnit, Project, SourceUnit } from "../model/project";

const ADVICE = "Call the target directly, or give the function behaviour of its own";

const DEFAULT_LIMIT = 80;
const PERCENT = 100;

export function FR02(project: Project): Finding[] {
  const limit = limitOf(project.config, "FR02", DEFAULT_LIMIT);
  return productionFiles(project)
    .flatMap((unit) => unit.functions.filter((fn) => isLayer(fn) && !isEffectWrapper(unit, fn)))
    .map((fn) => ({ fn, share: forwardedShare(fn.node) }))
    .filter((measured) => measured.share >= limit)
    .map((measured) => {
      const problem = `function ${measured.fn.name} is one call that forwards ${measured.share}% of its parameters`;
      return onSymbol("FR02", measured.fn, `${problem}, limit ${limit}%. ${ADVICE}`);
    });
}

/** Constructors and accessors are exempt; an anonymous function passed as a callback is not a layer of its own. */
function isLayer(fn: FunctionUnit): boolean {
  const isExempt =
    ts.isConstructorDeclaration(fn.node) || ts.isGetAccessorDeclaration(fn.node)
      || ts.isSetAccessorDeclaration(fn.node);
  return !fn.anonymous && !isExempt;
}

/** FX01 sends every effect through an effect component; a function there whose one call is an effect API is exempt. */
function isEffectWrapper(unit: SourceUnit, fn: FunctionUnit): boolean {
  const call = soleCall(fn.node.body);
  if (!unit.effectComponent || !call) return false;
  return fn.effects.some(
    (use) => use.node === call || ancestors(use.node).includes(call.expression) || use.node === call.expression);
}

/** The percentage of parameters passed unchanged to the one call that is the body; 0 when the body is not one call. */
function forwardedShare(fn: FunctionNode): number {
  const parameters = fn.parameters.filter((parameter) => !isThisParameter(parameter));
  const call = soleCall(fn.body);
  if (!call || parameters.length === 0) return 0;
  const passed =
    new Set(call.arguments.map((argument) => (ts.isSpreadElement(argument) ? argument.expression : argument)).filter(
      ts.isIdentifier).map((name) => name.text));
  const forwarded = parameters.filter((parameter) => passed.has(parameter.name.getText()));
  return Math.floor((PERCENT * forwarded.length) / parameters.length);
}

function soleCall(body: ts.ConciseBody): ts.CallExpression | undefined {
  const only = ts.isBlock(body) ? soleExpression(body) : body;
  const call = only ? awaited(only) : undefined;
  return call && ts.isCallExpression(call) ? call : undefined;
}

function soleExpression(body: ts.Block): ts.Expression | undefined {
  const statement = body.statements.length === 1 ? body.statements[0] : undefined;
  if (!statement) return undefined;
  return ts.isReturnStatement(statement) || ts.isExpressionStatement(statement) ? statement.expression : undefined;
}
