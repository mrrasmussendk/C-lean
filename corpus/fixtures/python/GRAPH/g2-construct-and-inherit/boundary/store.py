from typing import Protocol


class Store(Protocol):
    def save(self, item: str) -> None: ...


class MemoryStore(Store):
    def save(self, item: str) -> None:
        self.last = item
