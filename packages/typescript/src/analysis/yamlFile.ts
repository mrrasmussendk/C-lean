/** Reading values out of a YAML text while keeping the file, line and key for error messages. */
import { type Document, LineCounter, parseDocument } from "yaml";
import { type Failure, failure } from "../model/failure";
import type { YamlText } from "../model/inputs";

type Key = string | number;

export interface Section {
  file: string;
  document: Document;
  lines: LineCounter;
  path: readonly Key[];
}

export function openYaml(source: YamlText): Section {
  const lines = new LineCounter();
  const document = parseDocument(source.text, { lineCounter: lines });
  const broken = document.errors[0];
  if (broken) {
    const at = broken.linePos?.[0] ?? { line: 1, col: 1 };
    throw failure(`${source.file}:${at.line}:${at.col}`, `the YAML does not parse: ${firstLine(broken.message)}`, "Fix the YAML syntax at that position");
  }
  return { file: source.file, document, lines, path: [] };
}

export function child(section: Section, key: Key): Section {
  return { ...section, path: [...section.path, key] };
}

export function valueOf(section: Section): unknown {
  const node: unknown = section.document.getIn(section.path, true);
  return isNode(node) ? node.toJSON() : undefined;
}

/** The failure for a key whose value is wrong, placed at the key or, when it is absent, at its nearest parent. */
export function malformed(section: Section, problem: string, remedy: string): Failure {
  const key = section.path.length > 0 ? `key ${section.path.join(".")}` : "the document";
  return failure(`${section.file}:${positionOf(section)}`, `${key} ${problem}`, remedy);
}

export function mapping(section: Section): Section {
  const value = valueOf(section);
  const isMapping = typeof value === "object" && !Array.isArray(value);
  if (value !== undefined && !isMapping) throw malformed(section, "must be a mapping", "Write it as key: value pairs");
  return section;
}

export function keysOf(section: Section): string[] {
  const value = valueOf(mapping(section));
  return value ? Object.keys(value) : [];
}

export function items(section: Section): Section[] {
  const value = valueOf(section);
  if (value === undefined) return [];
  if (!Array.isArray(value)) throw malformed(section, "must be a list", "Write it as a YAML list");
  return value.map((_, index) => child(section, index));
}

export function text(section: Section): string {
  const value = valueOf(section);
  if (typeof value !== "string" || !value) throw malformed(section, "must be a non-empty string", "Give it a text value");
  return value;
}

export function wholeNumber(section: Section): number {
  const value = valueOf(section);
  if (typeof value !== "number" || !Number.isInteger(value)) throw malformed(section, "must be a whole number", "Give it as a number without quotes");
  return value;
}

export function texts(section: Section): string[] {
  return items(section).map(text);
}

function positionOf(section: Section): string {
  const paths = section.path.map((_, index) => section.path.slice(0, section.path.length - index));
  const nodes = paths.map((path): unknown => section.document.getIn(path, true));
  const start = nodes.filter(isNode).flatMap((node) => (node.range ? [node.range[0]] : []))[0] ?? 0;
  const at = section.lines.linePos(start);
  return `${at.line}:${at.col}`;
}

interface PositionedNode {
  range?: readonly number[];
  toJSON(): unknown;
}

function isNode(value: unknown): value is PositionedNode {
  return typeof value === "object" && !!value && "toJSON" in value;
}

function firstLine(message: string): string {
  return message.split("\n")[0].replace(/ at line \d+, column \d+:?$/, "");
}
