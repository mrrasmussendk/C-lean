from orders import total


def test_total() -> None:
    assert total(2, 3) == 6
    assert total(0, 3) == 0
    assert total(1, 1) == 1
