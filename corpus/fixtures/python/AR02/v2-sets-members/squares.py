class Order:
    def __init__(self) -> None:
        self.total = 0
        self.count = 0


def fill(
    order: Order,
    prices: list[int],
) -> None:
    order.total = sum(prices)
    order.count = len(prices)
