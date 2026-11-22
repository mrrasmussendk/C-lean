// CP01: Law of Demeter: a member-access chain must not pass through more than one behavioural object defined in the solution. Exempt: data records, standard-library types, fluent calls returning the receiver's type
import * as ts from "typescript";
import { onNode, productionFiles } from "../analysis/report";
import { limitOf } from "../analysis/settings";
import { descendants } from "../analysis/syntax";
import { isBehavioural } from "../analysis/types";
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";

const ADVICE = "Ask the first object for what you need instead of reaching through it";

const DEFAULT_LIMIT = 1;

type Link = ts.PropertyAccessExpression | ts.ElementAccessExpression | ts.CallExpression | ts.NonNullExpression;

export function CP01(project: Project): Finding[] {
  const limit = limitOf(project.config, "CP01", DEFAULT_LIMIT);
  return productionFiles(project).flatMap((unit) =>
    descendants(unit.source)
      .filter(isChainEnd)
      .map((chain) =>
        ({ chain, passed: linksOf(chain).filter((link) => isBehaviouralStop(link, project.checker)).length }))
      .filter((measured) => measured.passed > limit)
      .map((measured) =>
        onNode("CP01", unit.path, measured.chain,
          `member-access chain ${measured.chain.getText().split(
            "\n")[0]} passes through ${measured.passed} behavioural objects, limit ${limit}. ${ADVICE}`),
      ),
  );
}

function isLink(node: ts.Node): node is Link {
  return ts.isPropertyAccessExpression(node) || ts.isElementAccessExpression(node) || ts.isCallExpression(node)
    || ts.isNonNullExpression(node);
}

/** The outermost link of a chain: nothing continues the chain from it. */
function isChainEnd(node: ts.Node): node is Link {
  return isLink(node) && !(isLink(node.parent) && node.parent.expression === node);
}

/** The links of a chain below its end; the chain's first receiver is not a link. */
function linksOf(chain: Link): Link[] {
  const next = chain.expression;
  return isLink(next) ? [next, ...linksOf(next)] : [];
}

/** An intermediate result that is itself dereferenced, is behavioural, and is not a fluent call on its own type. */
function isBehaviouralStop(link: Link, checker: ts.TypeChecker): boolean {
  const user = ts.isNonNullExpression(link.parent) ? link.parent.parent : link.parent;
  const isDereferenced = ts.isPropertyAccessExpression(user) || ts.isElementAccessExpression(user);
  if (!isDereferenced || ts.isNonNullExpression(link)) return false;
  const type = checker.getNonNullableType(checker.getTypeAtLocation(link));
  return isBehavioural(type) && !isFluent(link, type, checker);
}

function isFluent(link: Link, type: ts.Type, checker: ts.TypeChecker): boolean {
  if (!ts.isCallExpression(link) || !ts.isPropertyAccessExpression(link.expression)) return false;
  const receiver = checker.getNonNullableType(checker.getTypeAtLocation(link.expression.expression));
  return receiver.getSymbol() === type.getSymbol();
}
