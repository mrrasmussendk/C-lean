def box(a: int, b: int, c: int, d: int, e: int) -> int:
    return a * b * c * d * e


def wrap(a: int, b: int, c: int, d: int, unused: int) -> int:
    return box(a, b, c, d, 1)
