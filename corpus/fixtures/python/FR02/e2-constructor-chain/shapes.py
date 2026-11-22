class Shape:
    def __init__(self, width: int, height: int) -> None:
        self.width = width
        self.height = height


class Square(Shape):
    def __init__(self, width: int, height: int) -> None:
        super().__init__(width, height)
