class Pricing:
    def total(self, price: int) -> int:
        return price * 2 - self._discount(price)

    def _discount(self, price: int) -> int:
        return price // 2
