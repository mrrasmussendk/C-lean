class Pricing:
    def total(self, price: int) -> int:
        return price * 2

    def _discount(self, price: int) -> int:
        return price // 2
