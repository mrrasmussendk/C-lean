from dataclasses import dataclass


@dataclass(frozen=True)
class Order:
    price: int


def cheapest(orders: list[Order]) -> list[Order]:
    return sorted(orders, key=str)[:1]
