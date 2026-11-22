from unittest.mock import Mock

from stock import available


def test_available() -> None:
    warehouse = Mock()
    warehouse.count.return_value = 5
    assert warehouse.count("nail") == 5
    assert available(warehouse, "nail") == 4
