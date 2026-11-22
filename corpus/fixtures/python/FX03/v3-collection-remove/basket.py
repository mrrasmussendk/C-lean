def empty(items: list[int], seen: set[int]) -> int:
    items.clear()
    seen.remove(1)
    return 0
