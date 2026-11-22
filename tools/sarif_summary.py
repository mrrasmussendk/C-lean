"""Prints the unsuppressed findings of a SARIF file. Exit 1 when there are any."""

import json
import sys


def main(path: str) -> int:
    with open(path, encoding="utf-8") as handle:
        runs = json.load(handle)["runs"]
    results = [result for run in runs for result in run.get("results", []) if not result.get("suppressions")]
    for result in results:
        location = result["locations"][0]["physicalLocation"]
        print(f"{location['artifactLocation']['uri']}:{location['region']['startLine']} {result['message']['text']}")
    return int(bool(results))


if __name__ == "__main__":
    sys.exit(main(sys.argv[1]))
