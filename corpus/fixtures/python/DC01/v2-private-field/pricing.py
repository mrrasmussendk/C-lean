class Pricing:
    def __init__(self) -> None:
        self.rate = 2
        self._spare = 0

    def total(self, price: int) -> int:
        return price * self.rate
