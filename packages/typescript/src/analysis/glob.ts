/** Path globs of gate.yaml and architecture.yaml: `**` any number of directories, `*` within one segment. */

export function matchesAny(globs: readonly string[], path: string): boolean {
  return globs.some((glob) => patternOf(glob).test(path));
}

function patternOf(glob: string): RegExp {
  const body = glob
    .split("**/")
    .map((part) => part.split("**").map(segmentPattern).join(".*"))
    .join("(?:.*/)?");
  return new RegExp(`^${body}$`);
}

function segmentPattern(text: string): string {
  return text.split("*").map(escaped).join("[^/]*");
}

function escaped(text: string): string {
  return text.replace(/[.+?^${}()|[\]\\]/g, "\\$&");
}
