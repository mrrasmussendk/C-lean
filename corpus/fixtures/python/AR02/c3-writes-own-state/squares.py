class Totals:
    def __init__(self) -> None:
        self.total = 0

    def record(self, prices: list[int]) -> None:
        self.total = sum(prices)
