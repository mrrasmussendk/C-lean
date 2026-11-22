from pricing import _discount


def test_discount() -> None:
    assert _discount(4) == 2
