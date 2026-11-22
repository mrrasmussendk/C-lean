class Wallet:
    def __init__(self) -> None:
        self.balance = 0

    def pay(self) -> None:
        self.balance -= 1


class Customer:
    def __init__(self) -> None:
        self.wallet = Wallet()

    def name(self) -> str:
        return "customer"


class Order:
    def __init__(self, customer: Customer) -> None:
        self.customer = customer

    def number(self) -> int:
        return 1


def settle(order: Order) -> None:
    order.customer.wallet.pay()
