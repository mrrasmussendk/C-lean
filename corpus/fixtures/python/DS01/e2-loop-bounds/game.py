FRAME_COUNT = 10


def total(scores: list[int]) -> int:
    result = 0
    for index in range(FRAME_COUNT):
        result += scores[index]
    return result


def remaining(played: int) -> int:
    return FRAME_COUNT - played


def countdown() -> list[int]:
    left = FRAME_COUNT
    numbers = []
    while left > 0:
        numbers.append(left)
        left -= 1
    return numbers
