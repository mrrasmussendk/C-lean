import pytest

from orders import total


@pytest.mark.parametrize("price, quantity", [(1, 2), (3, 4)])
def test_total(price: int, quantity: int) -> None:
    total(price, quantity)
