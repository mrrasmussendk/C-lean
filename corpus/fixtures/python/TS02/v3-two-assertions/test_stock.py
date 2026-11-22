from unittest.mock import MagicMock, Mock


def test_count() -> None:
    warehouse = Mock()
    warehouse.count.return_value = 5
    shelf = MagicMock(return_value=3)
    assert warehouse.count("nail") == 5
    assert shelf() == 3
