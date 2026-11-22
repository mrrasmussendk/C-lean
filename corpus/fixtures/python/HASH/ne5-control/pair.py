def report(*parts: object) -> None:
    del parts


def announce(*parts: object) -> None:
    del parts


def size(values: list[int]) -> int:
    return len(values)


def left(items: list[int], factor: int) -> int:
    total = 0
    count = len(items)
    for item in items:
        total = total + item * factor
    while total > count:
        total = total - count
    label = "sum"
    report(label, total)
    return total


def right(items: list[int], factor: int) -> int:
    total = 0
    count = len(items)
    for item in items:
        total = total + item * factor
    if total > count:
        total = total - count
    label = "sum"
    report(label, total)
    return total
