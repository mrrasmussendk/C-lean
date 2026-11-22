class Box:
    def resize(self, a: int, b: int, c: int, d: int) -> int:
        return a * b * c * d

    @classmethod
    def make(cls, a: int, b: int, c: int, d: int) -> int:
        return a + b + c + d
