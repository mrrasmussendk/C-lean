"""File system access for the harness. The only module that reads or writes files."""

import json
import shutil
from pathlib import Path
from typing import Any

import yaml

HEADER_LINES = 5


def load_yaml(path: Path) -> dict[str, Any]:
    with path.open(encoding="utf-8") as handle:
        loaded = yaml.safe_load(handle)
    return loaded if isinstance(loaded, dict) else {}


def load_json(path: Path) -> dict[str, Any]:
    with path.open(encoding="utf-8") as handle:
        loaded = json.load(handle)
    return loaded if isinstance(loaded, dict) else {}


def suite_files(cases_dir: Path) -> list[Path]:
    return sorted(cases_dir.glob("*.yaml"))


def directories(parent: Path) -> list[Path]:
    return sorted(path for path in parent.iterdir() if path.is_dir())


def exists(path: Path) -> bool:
    return path.exists()


def fresh_directory(path: Path) -> Path:
    shutil.rmtree(path, ignore_errors=True)
    path.mkdir(parents=True)
    return path


def rule_files(package_dir: Path, rule: str) -> list[Path]:
    return sorted(path for path in package_dir.glob(f"**/rules/{rule}.*") if "node_modules" not in path.parts)


def first_lines(path: Path) -> list[str]:
    with path.open(encoding="utf-8") as handle:
        return handle.read().splitlines()[:HEADER_LINES]
