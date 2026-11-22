class Strike:
    def __init__(self) -> None:
        self.pins = 10


class Spare:
    def __init__(self) -> None:
        self.pins = 10


def bonus(frame: object) -> int:
    if isinstance(frame, Strike):
        return 2
    return 0


def symbol(frame: object) -> str:
    return "X" if isinstance(frame, (Strike, int)) else "-"


def rolls(frame: object) -> int:
    match frame:
        case Strike():
            return 1
        case _:
            return 2
