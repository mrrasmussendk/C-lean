class Shape:
    def __init__(self) -> None:
        self.drawn = 0

    def place(self, left: int, top: int, width: int) -> None:
        self.drawn = left + top + width


class Box(Shape):
    def place(self, left: int, top: int, width: int) -> None:
        self.drawn = left * top * width


class Line(Shape):
    def place(self, left: int, top: int, width: int) -> None:
        self.drawn = left - top - width
