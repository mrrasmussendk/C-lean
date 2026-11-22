def _double(price: int) -> int:
    return price * 2


def totals(prices: list[int]) -> list[int]:
    return list(map(_double, prices))
