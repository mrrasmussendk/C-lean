from enum import Enum


class Mark(Enum):
    STRIKE = "strike"
    SPARE = "spare"
    OPEN = "open"


def bonus(mark: Mark) -> int:
    match mark:
        case Mark.STRIKE:
            return 2
        case Mark.SPARE:
            return 1
        case _:
            return 0


def symbol(mark: Mark) -> str:
    if mark == Mark.STRIKE:
        return "X"
    return "/" if mark == Mark.SPARE else "-"


def ends_frame(kind: Mark) -> bool:
    if kind != Mark.OPEN:
        return True
    return False
