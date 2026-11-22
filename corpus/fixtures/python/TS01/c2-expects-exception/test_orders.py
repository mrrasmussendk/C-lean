import pytest

from orders import total


def test_negative_price() -> None:
    with pytest.raises(ValueError):
        total(-1, 3)
