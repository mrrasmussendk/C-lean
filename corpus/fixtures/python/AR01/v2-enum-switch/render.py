from enum import Enum


class Mode(Enum):
    PLAIN = "plain"
    SALE = "sale"


def price(
    amount: int,
    mode: Mode,
) -> int:
    match mode:
        case Mode.SALE:
            return amount // 2
        case _:
            return amount


def label(
    text: str,
    style: Mode,
) -> str:
    if style == Mode.SALE:
        return text + "!"
    return text
