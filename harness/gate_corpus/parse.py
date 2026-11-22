"""Pure conversion of loaded YAML and JSON documents into the harness model."""

from typing import Any

from gate_corpus.model import Case, Edge, Finding, Graph, Node

Document = dict[str, Any]


def cases_of(document: Document) -> list[Case]:
    suite = str(document["suite"])
    return [_case(suite, row) for row in document["cases"]]


def _case(suite: str, row: Document) -> Case:
    rules = tuple(str(rule) for rule in row.get("rules", [suite]))
    return Case(suite, str(row["id"]), str(row["kind"]), rules)


def expected_findings(document: Document) -> frozenset[Finding]:
    rows = document.get("findings", [])
    return frozenset(Finding(str(r["rule"]), str(r["file"]), int(r["line"])) for r in rows)


def sarif_findings(document: Document) -> frozenset[Finding]:
    results = [result for run in document["runs"] for result in run.get("results", [])]
    return frozenset(_sarif_finding(result) for result in results if not result.get("suppressions"))


def _sarif_finding(result: Document) -> Finding:
    location = result["locations"][0]["physicalLocation"]
    uri = str(location["artifactLocation"]["uri"])
    return Finding(str(result["ruleId"]), uri, int(location["region"]["startLine"]))


def graph_of(document: Document) -> Graph:
    nodes = frozenset(_node(row) for row in document.get("nodes", []))
    edges = frozenset(_edge(row) for row in document.get("edges", []))
    return Graph(nodes, edges)


def _node(row: Document) -> Node:
    return Node(str(row["id"]), str(row["kind"]), str(row["component"]))


def _edge(row: Document) -> Edge:
    return Edge(str(row["from"]), str(row["to"]), str(row["kind"]))


def hashes_named(document: Document, name: str) -> list[str]:
    units = document.get("units", [])
    return [str(unit["hash"]) for unit in units if _function_name(str(unit["id"])) == name]


def _function_name(symbol: str) -> str:
    qualified = symbol.split("(")[0]
    return qualified.replace(":", ".").split(".")[-1]
