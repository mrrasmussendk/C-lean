class C:
    def run(self) -> int:
        return 1


class B:
    def __init__(self) -> None:
        self.c = C()

    def get_c(self) -> C:
        return self.c

    def run(self) -> int:
        return 2


class A:
    def __init__(self) -> None:
        self.b = B()

    def get_b(self) -> B:
        return self.b


def use(c: C) -> int:
    return c.run()
