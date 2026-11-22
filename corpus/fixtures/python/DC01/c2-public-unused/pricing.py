LIMIT = 10


class Pricing:
    def total(self, price: int) -> int:
        return price * 2


def discount(price: int) -> int:
    return price // 2
