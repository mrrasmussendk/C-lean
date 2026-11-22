class Counter:
    def __init__(self) -> None:
        self.last = 0

    def next_id(self) -> int:
        self.last += 1
        return self.last
