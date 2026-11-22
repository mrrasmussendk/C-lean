/** Questions put to the type checker. */
import * as ts from "typescript";

export function unionParts(type: ts.Type): ts.Type[] {
  return type.isUnion() ? type.types : [type];
}

export function someFlag(type: ts.Type, flags: ts.TypeFlags): boolean {
  return unionParts(type).some((part) => (part.flags & flags) !== 0);
}

export function everyFlag(type: ts.Type, flags: ts.TypeFlags): boolean {
  return unionParts(type).every((part) => (part.flags & flags) !== 0);
}

/** The declarations a name refers to, following an import to what it imports. */
export function declarationsAt(checker: ts.TypeChecker, name: ts.Node): ts.Declaration[] {
  const symbol = checker.getSymbolAtLocation(name);
  if (!symbol) return [];
  const target = symbol.flags & ts.SymbolFlags.Alias ? checker.getAliasedSymbol(symbol) : symbol;
  return target.declarations ?? [];
}

/** The declarations of a type that are written in the analyzed sources, not in a library. */
export function ownDeclarations(type: ts.Type): ts.Declaration[] {
  return unionParts(type)
    .flatMap((part) => part.getSymbol()?.declarations ?? [])
    .filter((declaration) => !declaration.getSourceFile().isDeclarationFile);
}

/** A type defined under the root that declares at least one method. */
export function isBehavioural(type: ts.Type): boolean {
  return ownDeclarations(type).some(
    (declaration) =>
      (ts.isClassLike(declaration) || ts.isInterfaceDeclaration(declaration)) &&
      declaration.members.some((member) => ts.isMethodDeclaration(member) || ts.isMethodSignature(member)),
  );
}

/** The type a function hands to its caller: the awaited type for an async function. */
export function returnedType(checker: ts.TypeChecker, fn: ts.SignatureDeclaration): ts.Type | undefined {
  const signature = checker.getSignatureFromDeclaration(fn);
  if (!signature) return undefined;
  const declared = checker.getReturnTypeOfSignature(signature);
  return checker.getAwaitedType(declared) ?? declared;
}
