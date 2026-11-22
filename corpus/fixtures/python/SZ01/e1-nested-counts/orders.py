def outer(values: list[int]) -> int:
    total = 0
    total += len(values) + 0
    total += len(values) + 1
    total += len(values) + 2
    total += len(values) + 3
    total += len(values) + 4
    total += len(values) + 5
    total += len(values) + 6
    total += len(values) + 7
    total += len(values) + 8
    total += len(values) + 9
    total += len(values) + 10
    total += len(values) + 11
    total += len(values) + 12
    total += len(values) + 13
    total += len(values) + 14
    total += len(values) + 15
    total += len(values) + 16
    def inner(extra: int) -> int:
        result = extra
        result += 0
        result += 1
        result += 2
        result += 3
        result += 4
        result += 5
        result += 6
        result += 7
        result += 8
        result += 9
        result += 10
        result += 11
        result += 12
        return result
    total += inner(1)
    return total
