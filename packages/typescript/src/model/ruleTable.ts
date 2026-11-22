/** The rule table of spec/rules.yaml that the package needs at run time: IDs, default severities and owners. */

export const SPEC_VERSION = "1.0";
export const PACKAGE_VERSION = "0.1.0";
export const LANGUAGE = "typescript";
export const TOOL_NAME = "gate-typescript";

export const DEFAULT_SEVERITY: ReadonlyMap<string, string> = new Map([
  ["SZ01", "block"], ["SZ02", "block"], ["SZ03", "block"], ["SZ04", "block"], ["SZ05", "warn"], ["SZ06", "report"],
  ["AR01", "block"], ["AR02", "block"],
  ["FX01", "block"], ["FX02", "warn"], ["FX03", "warn"],
  ["DU01", "block"],
  ["ER01", "block"], ["ER02", "block"], ["ER03", "report"],
  ["CP01", "warn"],
  ["DC01", "block"], ["DC02", "block"], ["DC03", "warn"],
  ["FR01", "warn"], ["FR02", "warn"], ["FR03", "warn"], ["FR04", "report"],
  ["NM01", "warn"],
  ["DS01", "report"], ["DS02", "report"],
  ["TS01", "block"], ["TS02", "block"], ["TS03", "block"],
  ["HD01", "block"], ["HD02", "block"], ["HD03", "block"],
  ["GR01", "block"],
]);

/** Rules decided by the core; this package cannot know whether a suppression of one silences anything. */
export const CORE_RULES: ReadonlySet<string> = new Set(["DU01", "FR01", "FR03", "FR04", "TS03", "HD02", "HD03"]);

export const SARIF_LEVEL: ReadonlyMap<string, string> = new Map([
  ["block", "error"],
  ["warn", "warning"],
  ["report", "note"],
]);
