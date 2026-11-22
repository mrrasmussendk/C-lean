def step(index: int, values: list[int]) -> int:
    if index == 0:
        return values[-1]
    return values[index - 1] + 1
