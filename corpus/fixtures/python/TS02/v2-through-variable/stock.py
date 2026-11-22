from typing import Protocol


class Warehouse(Protocol):
    def count(self, item: str) -> int: ...


def available(warehouse: Warehouse, item: str) -> int:
    return warehouse.count(item) - 1
