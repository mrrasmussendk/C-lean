class Basket:
    def __init__(self) -> None:
        self.items: list[int] = []

    def add(self, item: int) -> None:
        self.items.append(item)


def fill(basket: Basket) -> int:
    basket.add(1)
    return 1
