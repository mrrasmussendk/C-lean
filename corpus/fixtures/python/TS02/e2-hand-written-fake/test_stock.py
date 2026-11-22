class FakeWarehouse:
    def count(self, item: str) -> int:
        return 5


def test_count() -> None:
    warehouse = FakeWarehouse()
    assert warehouse.count("nail") == 5
