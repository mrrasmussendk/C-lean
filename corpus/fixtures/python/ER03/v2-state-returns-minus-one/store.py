class Stack:
    def __init__(self) -> None:
        self.size = 0

    def push(self, limit: int) -> int:
        if self.size >= limit:
            return -1
        self.size += 1
        return self.size
