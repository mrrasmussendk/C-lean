PIN_ROWS = ((7, 8, 9, 10), (4, 5, 6), (2, 3), (1,))
PIN_SCORES = {7: [10, 20], 8: [30]}


def back_row() -> tuple[int, ...]:
    return PIN_ROWS[0]
