from stock import available


class Shelf:
    def count(self, item: str) -> int:
        return len(item)


def test_available() -> None:
    assert available(Shelf(), "nail") == 3
