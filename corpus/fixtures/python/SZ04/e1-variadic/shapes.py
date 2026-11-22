def volume(a: int, b: int, c: int, *rest: int) -> int:
    return a * b * c * len(rest)
