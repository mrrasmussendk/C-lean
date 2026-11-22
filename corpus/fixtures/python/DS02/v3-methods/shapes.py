class Canvas:
    def __init__(self) -> None:
        self.drawn = 0

    def box(self, left: int, top: int, width: int) -> None:
        self.drawn = left + top + width

    def line(self, left: int, top: int, width: int) -> None:
        self.drawn = left - top - width


class Printer:
    def __init__(self) -> None:
        self.printed = 0

    def strip(self, left: int, top: int, width: int) -> None:
        self.printed = left * top * width
