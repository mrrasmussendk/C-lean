def collect(
    source: list[int],
    results: list[int],
) -> None:
    for value in source:
        results.append(value * value)
