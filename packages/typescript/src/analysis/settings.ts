/** Validation of gate.yaml, architecture.yaml and the glossary, and the lookups rules make in them. */
import type { Component, GateConfig, GlossaryTerm, RuleSetting } from "../model/config";
import type { YamlText } from "../model/inputs";
import { DEFAULT_SEVERITY, SARIF_LEVEL } from "../model/ruleTable";
import {
  type Section,
  child,
  items,
  keysOf,
  malformed,
  mapping,
  openYaml,
  text,
  texts,
  valueOf,
  wholeNumber,
} from "./yamlFile";

export function parseConfig(source: YamlText): GateConfig {
  const root = mapping(openYaml(source));
  const paths = mapping(child(root, "paths"));
  const rules = child(root, "rules");
  return {
    tests: texts(child(paths, "tests")),
    generated: texts(child(paths, "generated")),
    rules: new Map(keysOf(rules).map((rule) => [rule, ruleSetting(child(rules, rule))])),
  };
}

function ruleSetting(section: Section): RuleSetting {
  const rule = String(section.path[section.path.length - 1]);
  if (!DEFAULT_SEVERITY.has(rule)) throw malformed(section, "is not a rule of the spec",
    "Use a rule ID from spec/rules.yaml");
  mapping(section);
  return { limit: limitSetting(child(section, "limit")), severity: severitySetting(child(section, "severity")) };
}

function limitSetting(section: Section): number | undefined {
  const value = valueOf(section);
  if (value !== undefined
    && typeof value !== "number") throw malformed(section, "must be a number", "Give the limit as a number");
  return value;
}

function severitySetting(section: Section): string | undefined {
  const value = valueOf(section);
  if (value === undefined) return undefined;
  if (typeof value !== "string"
    || !(SARIF_LEVEL.has(value) || value === OFF)) throw malformed(section, "must be block, warn, report or off",
      "Use one of the four");
  return value;
}

export function parseArchitecture(source: YamlText): Component[] {
  const components = child(mapping(openYaml(source)), "components");
  if (valueOf(components) === undefined) throw malformed(components, "is missing",
    "List the components of the architecture model");
  return items(components).map(component);
}

function component(section: Section): Component {
  const effect = valueOf(child(section, "effect")) ?? false;
  if (typeof effect !== "boolean") throw malformed(child(section, "effect"), "must be true or false",
    "Mark an effect component with effect: true");
  return { name: text(child(section, "name")), paths: texts(child(section, "paths")), effect };
}

export function parseGlossary(source: YamlText | undefined): GlossaryTerm[] {
  if (!source) return [];
  return items(child(mapping(openYaml(source)), "terms")).map(glossaryTerm);
}

function glossaryTerm(section: Section): GlossaryTerm {
  return { term: text(child(section, "term")), banned: texts(child(section, "banned")) };
}

export interface LineRange {
  path: string;
  start: number;
  end: number;
}

/** The `--lines` file of the mutants command; undefined when none was given, which means every line. */
export function parseLineRanges(source: YamlText | undefined): LineRange[] | undefined {
  if (!source) return undefined;
  return items(child(mapping(openYaml(source)), "lines")).map((row) => ({
    path: text(child(row, "path")),
    start: wholeNumber(child(row, "start")),
    end: wholeNumber(child(row, "end")),
  }));
}

export function limitOf(config: GateConfig, rule: string, fallback: number): number {
  return config.rules.get(rule)?.limit ?? fallback;
}

/** Whether a human switched the rule off in gate.yaml: its findings are then not written at all. */
export function isOff(config: GateConfig, rule: string): boolean {
  return config.rules.get(rule)?.severity === OFF;
}

const OFF = "off";

/** The SARIF level of a rule after applying the configured severity. */
export function levelOf(config: GateConfig, rule: string): string {
  const severity = config.rules.get(rule)?.severity ?? DEFAULT_SEVERITY.get(rule) ?? "block";
  return SARIF_LEVEL.get(severity) ?? "error";
}
