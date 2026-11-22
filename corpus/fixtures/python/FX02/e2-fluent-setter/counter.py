class Query:
    def __init__(self) -> None:
        self.limit = 0

    def with_limit(self, limit: int) -> "Query":
        self.limit = limit
        return self
