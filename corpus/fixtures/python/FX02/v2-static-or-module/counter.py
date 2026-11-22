latest = 0


def remember(value: int) -> int:
    global latest
    latest = value
    return value * 2
