"""Prints the unsuppressed findings of a SARIF file that stop a merge. Exit 1 when there are any.

Report-level findings (SARIF level `note`) never stop a change, so they are counted, not listed.
"""

import json
import sys

STOPPING = ("error", "warning")


def main(path: str) -> int:
    with open(path, encoding="utf-8") as handle:
        runs = json.load(handle)["runs"]
    results = [result for run in runs for result in run.get("results", []) if not result.get("suppressions")]
    stopping = [result for result in results if result.get("level") in STOPPING]
    for result in stopping:
        location = result["locations"][0]["physicalLocation"]
        print(f"{location['artifactLocation']['uri']}:{location['region']['startLine']} {result['message']['text']}")
    reports = len(results) - len(stopping)
    if reports:
        print(f"({reports} report-level findings, which do not stop a change)")
    return int(bool(stopping))


if __name__ == "__main__":
    sys.exit(main(sys.argv[1]))
