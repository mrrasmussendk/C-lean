// DC02: Comments must not contain code
import { commentedCode } from "../analysis/commentedCode";
import { onLine } from "../analysis/report";
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";

const ADVICE = "Delete it; version control keeps the old code";

export function DC02(project: Project): Finding[] {
  return project.files.flatMap((unit) =>
    commentedCode(unit.comments).map((comment) =>
      onLine("DC02", unit, comment.line,
        `comment on lines ${comment.line} to ${comment.endLine} holds code. ${ADVICE}`),
    ),
  );
}
