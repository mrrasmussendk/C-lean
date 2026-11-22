// NM01: Glossary: an identifier must not use a word the glossary lists as a banned synonym of a domain term
import * as ts from "typescript";
import { onNode, productionFiles } from "../analysis/report";
import { descendants } from "../analysis/syntax";
import type { GlossaryTerm } from "../model/config";
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";

const PLURAL_ENDINGS: readonly string[] = ["", "s", "es"];

interface Hit {
  word: string;
  term: string;
}

export function NM01(project: Project): Finding[] {
  return productionFiles(project).flatMap((unit) =>
    descendants(unit.source)
      .filter(isDeclaredName)
      .flatMap((name) =>
        bannedWords(project.glossary, name.text).map((hit) =>
          onNode("NM01", unit.path, name, wordingOf(name.text, hit)),
        ),
      ),
  );
}

function wordingOf(identifier: string, hit: Hit): string {
  const problem = `identifier ${identifier} uses "${hit.word}", a banned synonym of the domain term "${hit.term}"`;
  return `${problem}. Rename it to use ${hit.term}`;
}

/** The rule checks declarations, not uses: an identifier that is the name of the declaration holding it. */
function isDeclaredName(node: ts.Node): node is ts.Identifier {
  const holder = node.parent;
  const declares =
    !ts.isPropertyAccessExpression(holder) && !ts.isPropertyAssignment(holder)
      && (holder as ts.NamedDeclaration).name === node;
  return ts.isIdentifier(node) && declares;
}

/** Words are split on case changes, underscores and digits and compared whole, without case; a plural matches. */
function bannedWords(glossary: readonly GlossaryTerm[], identifier: string): Hit[] {
  const words = (identifier.match(/[A-Z]+(?![a-z])|[A-Z]?[a-z]+/g) ?? []).map((word) => word.toLowerCase());
  return glossary.flatMap((entry) =>
    entry.banned
      .filter((banned) => PLURAL_ENDINGS.some((ending) => words.includes(`${banned.toLowerCase()}${ending}`)))
      .map((banned) => ({ word: banned, term: entry.term })),
  );
}
