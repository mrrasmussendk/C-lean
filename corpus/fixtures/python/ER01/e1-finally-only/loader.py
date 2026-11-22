class Loader:
    def __init__(self) -> None:
        self.done = False

    def load(self, text: str) -> int:
        try:
            return int(text)
        finally:
            self.done = True
