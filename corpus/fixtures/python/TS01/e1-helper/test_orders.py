from orders import total


def check_total(price: int, quantity: int, expected: int) -> None:
    assert total(price, quantity) == expected


def test_total() -> None:
    check_total(2, 3, 6)
