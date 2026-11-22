from dataclasses import dataclass


@dataclass(frozen=True)
class Box:
    a: int
    b: int
    c: int
    d: int
    e: int
    f: int


def volume(box: Box) -> int:
    return box.a * box.b * box.c * box.d * box.e * box.f
