"""Runs a native package. The only module that starts processes."""

import subprocess
from dataclasses import dataclass
from pathlib import Path

TODAY = "2026-01-01"


@dataclass(frozen=True)
class Analysis:
    command: Path
    config: Path
    architecture: Path


@dataclass(frozen=True)
class Result:
    code: int
    output: str


def run(arguments: list[str]) -> Result:
    completed = subprocess.run(arguments, capture_output=True, text=True, check=False)
    return Result(completed.returncode, completed.stdout)


def analyze(analysis: Analysis, root: Path, out: Path) -> str:
    arguments = [str(analysis.command), "analyze", "--root", str(root), "--out", str(out)]
    arguments += ["--config", str(analysis.config), "--architecture", str(analysis.architecture)]
    arguments += ["--today", TODAY]
    completed = subprocess.run(arguments, capture_output=True, text=True, check=False)
    if completed.returncode == 0:
        return ""
    return f"exit code {completed.returncode}: {completed.stderr.strip()[-400:]}"
