def extend(items: list[int], prices: dict[str, int]) -> int:
    items.append(1)
    prices["extra"] = 1
    return len(items)
