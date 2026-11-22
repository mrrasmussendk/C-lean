class Pricing:
    def total(self, price: int) -> int:
        return self._adjust(price)

    def _adjust(self, price: int) -> int:
        raise NotImplementedError


class SalePricing(Pricing):
    def _adjust(self, price: int) -> int:
        return price // 2
