class Desk:
    def open(self) -> int:
        return 1


class Assistant:
    def __init__(self) -> None:
        self.desk = Desk()

    def greet(self) -> str:
        return "hello"


class Manager:
    def assistant(self) -> Assistant:
        return Assistant()


class Shop:
    def __init__(self) -> None:
        self.manager = Manager()

    def close(self) -> None:
        self.closed = True


def visit(shop: Shop) -> int:
    return shop.manager.assistant().desk.open()
