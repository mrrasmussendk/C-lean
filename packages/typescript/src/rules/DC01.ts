// DC01: A non-public symbol must be referenced by production code
import * as ts from "typescript";
import { onNode, productionFiles } from "../analysis/report";
import { descendants, hasModifier } from "../analysis/syntax";
import { declarationsAt } from "../analysis/types";
import { isPrivateMember } from "../analysis/visibility";
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";

const ADVICE = "Remove it, or use it from production code";

type Named = ts.Declaration & { name: ts.Node };

export function DC01(project: Project): Finding[] {
  const referenced =
    new Set(productionFiles(project).flatMap(
      (unit) => descendants(unit.source).flatMap((node) => referencesAt(node, project.checker))));
  return productionFiles(project).flatMap((unit) =>
    nonPublicDeclarations(unit.source)
      .filter((declaration) => !referenced.has(declaration))
      .map((declaration) =>
        onNode("DC01", unit.path, declaration.name,
          `non-public symbol ${declaration.name.getText()} has 0 references in production code. ${ADVICE}`),
      ),
  );
}

/** The declarations a name refers to; none when the name is the declaration's own name. */
function referencesAt(node: ts.Node, checker: ts.TypeChecker): ts.Declaration[] {
  if (!ts.isIdentifier(node) && !ts.isPrivateIdentifier(node)) return [];
  const declarations = localTarget(node, checker)?.declarations ?? declarationsAt(checker, node);
  return declarations.some((declaration) => ts.getNameOfDeclaration(declaration) === node) ? [] : declarations;
}

/** `{ name }` and `export { name }` declare a property or an export; what they use is the local they name. */
function localTarget(node: ts.Node, checker: ts.TypeChecker): ts.Symbol | undefined {
  const user = node.parent;
  if (ts.isShorthandPropertyAssignment(user)) return checker.getShorthandAssignmentValueSymbol(user);
  return ts.isExportSpecifier(user) ? checker.getExportSpecifierLocalTargetSymbol(user) : undefined;
}

/** Non-exported top-level declarations and private class members. Locals and parameters are out of scope. */
function nonPublicDeclarations(source: ts.SourceFile): Named[] {
  const hidden: ReadonlySet<ts.SyntaxKind> = new Set([ts.SyntaxKind.ExportKeyword, ts.SyntaxKind.DeclareKeyword]);
  const topLevel =
    source.statements.filter((statement) => ![...hidden].some((modifier) => hasModifier(statement, modifier)));
  const members =
    descendants(source).filter(ts.isClassLike).flatMap((type) => [...type.members, ...parameterProperties(type)]);
  return [...topLevel.flatMap(declaredBy), ...members.filter(isPrivateMember)].filter(isNamed);
}

function declaredBy(statement: ts.Statement): ts.Declaration[] {
  if (ts.isVariableStatement(statement)) return [...statement.declarationList.declarations];
  const isDeclaration =
    ts.isFunctionDeclaration(statement) || ts.isClassDeclaration(statement) || ts.isInterfaceDeclaration(statement)
      || ts.isTypeAliasDeclaration(statement) || ts.isEnumDeclaration(statement);
  return isDeclaration ? [statement] : [];
}

function parameterProperties(type: ts.ClassLikeDeclaration): ts.ParameterDeclaration[] {
  return type.members.filter(ts.isConstructorDeclaration).flatMap((constructor) => [...constructor.parameters]);
}

function isNamed(declaration: ts.Declaration): declaration is Named {
  const name = ts.getNameOfDeclaration(declaration);
  return name !== undefined && (ts.isIdentifier(name) || ts.isPrivateIdentifier(name));
}
