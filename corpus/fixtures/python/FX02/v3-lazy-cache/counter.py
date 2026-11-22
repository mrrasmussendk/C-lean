class Report:
    def __init__(self, rows: list[int]) -> None:
        self.rows = rows
        self.cached: int | None = None

    def total(self) -> int:
        if self.cached is None:
            self.cached = sum(self.rows)
        return self.cached
