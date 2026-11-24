/** Runs every rule and applies severity and suppression to what they find. */
import type { Finding, Verdict } from "../model/finding";
import type { Project } from "../model/project";
import { HD01 } from "../rules/HD01";
import { RULES } from "../rules/index";
import { isOff, levelOf } from "./settings";
import { defectOf, silences } from "./suppressions";

export function judge(project: Project): Verdict[] {
  const found = RULES.flatMap((rule) => rule(project));
  return [...found, ...HD01(project, found)]
    .filter((finding) => !isOff(project.config, finding.rule))
    .map((finding) =>
      ({ finding, level: levelOf(project.config, finding.rule), suppressed: isSuppressed(project, finding) }))
    .sort(byPlace);
}

/** Silenced by a complete, known and unexpired suppression in the same file. */
function isSuppressed(project: Project, finding: Finding): boolean {
  const unit = project.files.find((candidate) => candidate.path === finding.file);
  const valid = (unit?.suppressions ?? []).filter((suppression) => !defectOf(suppression, project.today));
  return valid.some((suppression) => silences(suppression, finding));
}

function byPlace(left: Verdict, right: Verdict): number {
  const leftKey =
    [left.finding.file, String(left.finding.line).padStart(LINE_DIGITS, "0"), left.finding.rule].join(" ");
  const rightKey =
    [right.finding.file, String(right.finding.line).padStart(LINE_DIGITS, "0"), right.finding.rule].join(" ");
  if (leftKey === rightKey) return 0;
  return leftKey < rightKey ? -1 : 1;
}

const LINE_DIGITS = 9;
