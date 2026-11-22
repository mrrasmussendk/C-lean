/** Builds the three interchange documents. Pure: the writer puts them on disk. */
import type { Graph } from "../analysis/graph";
import type { UnitRecord } from "../analysis/unitRecords";
import type { Verdict } from "../model/finding";
import { LANGUAGE, PACKAGE_VERSION, SPEC_VERSION, TOOL_NAME } from "../model/ruleTable";

const SARIF_SCHEMA = "https://json.schemastore.org/sarif-2.1.0.json";
const SARIF_VERSION = "2.1.0";

export function sarifDocument(verdicts: readonly Verdict[]): unknown {
  const driver = { name: TOOL_NAME, version: PACKAGE_VERSION };
  return { $schema: SARIF_SCHEMA, version: SARIF_VERSION, runs: [{ tool: { driver }, results: verdicts.map(sarifResult) }] };
}

function sarifResult(verdict: Verdict): unknown {
  const finding = verdict.finding;
  const location = { artifactLocation: { uri: finding.file }, region: { startLine: finding.line } };
  return {
    ruleId: finding.rule,
    level: verdict.level,
    message: { text: finding.message },
    locations: [{ physicalLocation: location }],
    partialFingerprints: { "gate/v1": `${finding.rule}|${finding.file}|${finding.anchor}` },
    ...(verdict.suppressed ? { suppressions: [{ kind: "inSource" }] } : {}),
  };
}

export function graphDocument(graph: Graph): unknown {
  return { spec: SPEC_VERSION, language: LANGUAGE, nodes: graph.nodes, edges: graph.edges };
}

export function unitsDocument(units: readonly UnitRecord[]): unknown {
  return { spec: SPEC_VERSION, language: LANGUAGE, units };
}
