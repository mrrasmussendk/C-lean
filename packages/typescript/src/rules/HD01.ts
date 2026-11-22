// HD01: An inline suppression must name the rule ID, a reason, an expiry date and an approver
import { onLine } from "../analysis/report";
import { SUPPRESSION_FORM, defectOf, isIdle, suppressionsOf, toolSwitchOf } from "../analysis/suppressions";
import type { Finding } from "../model/finding";
import type { Project, SourceUnit, Suppression } from "../model/project";

/** Takes the findings of the other rules as well: a suppression that silences none of them is a violation. */
export function HD01(project: Project, findings: readonly Finding[]): Finding[] {
  return project.files.flatMap((unit) => {
    const inFile = findings.filter((finding) => finding.file === unit.path);
    const switches = unit.comments.flatMap((comment) => {
      const used = toolSwitchOf(comment);
      return used ? [onLine("HD01", unit, comment.line, `comment uses the tool switch ${used}, which is not an accepted suppression. Fix the code, or write ${SUPPRESSION_FORM}`)] : [];
    });
    const malformed = suppressionsOf(unit.comments).flatMap((suppression) => {
      const defect = defectOf(suppression, project.today) ?? idleDefect(suppression, inFile);
      return defect ? [onSuppression(unit, suppression, defect)] : [];
    });
    return [...switches, ...malformed];
  });
}

function idleDefect(suppression: Suppression, findings: readonly Finding[]): string | undefined {
  return isIdle(suppression, findings) ? `of ${suppression.rule} silences 0 findings on its line and the line below` : undefined;
}

function onSuppression(unit: SourceUnit, suppression: Suppression, defect: string): Finding {
  return onLine("HD01", unit, suppression.line, `suppression ${defect}. Remove it, or write it as ${SUPPRESSION_FORM}`);
}
