from unittest.mock import Mock


def test_count() -> None:
    warehouse = Mock()
    warehouse.count.return_value = 5
    result = warehouse.count("nail")
    assert result == 5
