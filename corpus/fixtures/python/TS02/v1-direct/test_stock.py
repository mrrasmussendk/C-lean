from unittest.mock import Mock


def test_count() -> None:
    warehouse = Mock()
    warehouse.count.return_value = 5
    assert warehouse.count("nail") == 5
