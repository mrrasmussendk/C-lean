"""The command line: gate-python analyze | mutants | instrument, each with its own arguments."""

from pathlib import PurePath
from typing import Mapping

from gate_python import analyze
from gate_python.effects import console, reader, writer
from gate_python.model import InputError, Options, Problem
from gate_python.rule_table import PACKAGE_VERSION, SPEC_VERSION

SUCCESS = 0
FAILURE = 2
PAIR = 2
ANALYZE = "analyze"
MUTANTS = "mutants"
INSTRUMENT = "instrument"
NO_DATE = "0001-01-01"
REQUIRED: Mapping[str, tuple[str, ...]] = {
    ANALYZE: ("--root", "--out", "--config", "--architecture", "--today"),
    MUTANTS: ("--root", "--out", "--config", "--architecture"),
    INSTRUMENT: ("--root", "--config", "--architecture", "--trace-file"),
}
OPTIONAL: Mapping[str, tuple[str, ...]] = {ANALYZE: (), MUTANTS: ("--lines",), INSTRUMENT: ()}
USAGE = "Run gate-python analyze|mutants|instrument --root <dir> --config <gate.yaml> --architecture <architecture.yaml> and the command's own arguments (see spec/native-contract.md)"


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
    inputs = reader.load(options)
    if options.command == INSTRUMENT:
        outputs = analyze.instrumented(inputs, options.trace_file)
    elif options.command == MUTANTS:
        outputs = analyze.mutants_of(inputs)
    else:
        outputs = analyze.outputs_of(inputs)
    writer.write(options.out or options.root, outputs)
    console.report(list(outputs.problems))
    return FAILURE if outputs.problems else SUCCESS


def _options(arguments: list[str]) -> Options:
    command = "".join(arguments[:1])
    if command not in REQUIRED:
        raise InputError(Problem("", 0, 0, f"the command must be one of {', '.join(REQUIRED)}. {USAGE}"))
    given = dict(zip(arguments[1::PAIR], arguments[PAIR::PAIR]))
    unknown = [name for name in arguments[1::PAIR] if name not in (*REQUIRED[command], *OPTIONAL[command])]
    missing = [name for name in REQUIRED[command] if name not in given]
    if unknown or missing:
        raise InputError(Problem("", 0, 0, f"{_complaint(command, unknown, missing)}. {USAGE}"))
    trace_file = given.get("--trace-file", "")
    if trace_file and not PurePath(trace_file).is_absolute():
        raise InputError(Problem("", 0, 0, f"--trace-file is '{trace_file}' but must be an absolute path. Pass the full path of the trace file"))
    return Options(
        command=command,
        root=given["--root"],
        out=given.get("--out", ""),
        config=given["--config"],
        architecture=given["--architecture"],
        today=given.get("--today", NO_DATE),
        lines=given.get("--lines", ""),
        trace_file=trace_file,
    )


def _complaint(command: str, unknown: list[str], missing: list[str]) -> str:
    if unknown:
        return f"{command} does not take the argument {unknown[0]}"
    return f"{command} lacks the argument {missing[0]} or its value"
