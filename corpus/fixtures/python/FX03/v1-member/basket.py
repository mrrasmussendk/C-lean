class Order:
    def __init__(self) -> None:
        self.total = 0


def reset(order: Order) -> int:
    order.total = 0
    return 0
