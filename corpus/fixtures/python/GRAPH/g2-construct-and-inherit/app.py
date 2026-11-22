from boundary.store import MemoryStore


class Checkout:
    def __init__(self) -> None:
        self.store = MemoryStore()
