class Loader:
    def __init__(self) -> None:
        self.failed = False

    def load(self, text: str) -> int:
        try:
            return int(text)
        except ValueError:
            self.failed = True
            return 0
