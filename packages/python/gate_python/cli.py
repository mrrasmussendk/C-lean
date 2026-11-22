"""The command line: gate-python analyze --root <dir> --out <dir> --config <file> --architecture <file> --today <date>."""

from gate_python import analyze
from gate_python.effects import console, reader, writer
from gate_python.model import InputError, Options, Problem
from gate_python.rule_table import PACKAGE_VERSION, SPEC_VERSION

SUCCESS = 0
FAILURE = 2
PAIR = 2
COMMAND = "analyze"
REQUIRED = ("--root", "--out", "--config", "--architecture", "--today")
USAGE = "Run gate-python analyze --root <dir> --out <dir> --config <gate.yaml> --architecture <architecture.yaml> --today <YYYY-MM-DD>"


def main(arguments: list[str]) -> int:
    try:
        return _run(arguments)
    except InputError as error:
        console.report([error.problem])
        return FAILURE
    except Exception as error:
        console.report([Problem("", 0, 0, f"internal error, {type(error).__name__}: {error}. Report this as a bug in gate-python")])
        return FAILURE


def _run(arguments: list[str]) -> int:
    if arguments == ["--version"]:
        console.say(f"{PACKAGE_VERSION} spec {SPEC_VERSION}")
        return SUCCESS
    options = _options(arguments)
    outputs = analyze.outputs_of(reader.load(options))
    writer.write(options.out, outputs)
    console.report(list(outputs.problems))
    return FAILURE if outputs.problems else SUCCESS


def _options(arguments: list[str]) -> Options:
    if arguments[:1] != [COMMAND]:
        raise InputError(Problem("", 0, 0, f"the command must be '{COMMAND}'. {USAGE}"))
    given = dict(zip(arguments[1::PAIR], arguments[PAIR::PAIR]))
    unknown = [name for name in arguments[1::PAIR] if name not in REQUIRED]
    missing = [name for name in REQUIRED if name not in given]
    if unknown or missing:
        raise InputError(Problem("", 0, 0, f"{_complaint(unknown, missing)}. {USAGE}"))
    return Options(given["--root"], given["--out"], given["--config"], given["--architecture"], given["--today"])


def _complaint(unknown: list[str], missing: list[str]) -> str:
    if unknown:
        return f"unknown argument {unknown[0]}"
    return f"missing argument {missing[0]} or its value"
