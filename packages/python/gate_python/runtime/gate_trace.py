"""Runtime helper of the gate's trace check: records which component calls which. Standard library only."""

import contextvars
import json
import threading
from typing import Callable

TRACE_FILE = "__GATE_TRACE_FILE__"
CURRENT: contextvars.ContextVar[str] = contextvars.ContextVar("gate_component", default="")
SEEN: set[tuple[str, str]] = set()
LOCK = threading.Lock()


def enter(component: str) -> str:
    """Makes the component current and returns the one that was; a change between two named components is an edge."""
    previous = CURRENT.get()
    if previous and component and previous != component:
        record(previous, component)
    CURRENT.set(component)
    return previous


def leave(previous: str | None) -> None:
    """Makes the previous component current again; does nothing for a generator that is suspended."""
    if previous is not None:
        CURRENT.set(previous)


def suspend(previous: str | None, value: object) -> object:
    """Hands the current component back while a generator is suspended at a yield."""
    CURRENT.set(previous or "")
    return value


def call(component: str, body: Callable[[], object]) -> object:
    """Runs the body of an anonymous function inside the component."""
    previous = enter(component)
    try:
        return body()
    finally:
        CURRENT.set(previous)


def record(source: str, target: str) -> None:
    """Appends the edge to the trace file as one whole line, once per process."""
    with LOCK:
        if (source, target) in SEEN:
            return
        SEEN.add((source, target))
        with open(TRACE_FILE, "a", encoding="utf-8") as handle:
            handle.write(json.dumps({"from": source, "to": target}) + "\n")
