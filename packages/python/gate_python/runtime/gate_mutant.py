"""Runtime helper of the gate's mutant switching: says which mutant is on and records the ones reached.

Standard library only.
"""

import os
import threading

IDS: list[str] = ["__GATE_MUTANT_IDS__"]
WANTED = os.environ.get("GATE_MUTANT", "")
ACTIVE = IDS.index(WANTED) if WANTED in IDS else -1
REACHED_FILE = os.environ.get("GATE_MUTANT_REACHED", "")
SEEN = bytearray(len(IDS))
LOCK = threading.Lock()


def on(index: int) -> bool:
    """True when the mutant with this index is the active one; the first evaluation of each switch is recorded."""
    if not SEEN[index]:
        reach(index)
    return index == ACTIVE


def reach(index: int) -> None:
    """Marks the mutant as reached and, when asked to, appends its id to the reached file as one whole line."""
    with LOCK:
        if SEEN[index]:
            return
        SEEN[index] = 1
        if REACHED_FILE:
            with open(REACHED_FILE, "a", encoding="utf-8") as handle:
                handle.write(IDS[index] + "\n")
