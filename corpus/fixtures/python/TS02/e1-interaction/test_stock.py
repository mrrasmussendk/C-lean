from unittest.mock import Mock

from stock import available


def test_available() -> None:
    warehouse = Mock()
    warehouse.count.return_value = 5
    available(warehouse, "nail")
    warehouse.count.assert_called_with("nail")
