from dataclasses import dataclass


@dataclass(frozen=True)
class City:
    name: str


@dataclass(frozen=True)
class Address:
    city: City


@dataclass(frozen=True)
class Customer:
    address: Address


@dataclass(frozen=True)
class Order:
    customer: Customer


def city_of(order: Order) -> str:
    return order.customer.address.city.name.upper()
