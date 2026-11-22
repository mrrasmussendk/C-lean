class Basket:
    def __init__(self) -> None:
        self.items: list[int] = []

    def add(self, item: int) -> None:
        self.items.append(item)
        self.count = len(self.items)
