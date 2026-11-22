FRAME_COUNT = 10


def bonus(frame: int) -> int:
    if frame == FRAME_COUNT:
        return 2
    return 0


def is_over(played: int) -> bool:
    if played >= FRAME_COUNT:
        return True
    return False


def label(index: int) -> str:
    return "last" if index + 1 > FRAME_COUNT - 1 else "frame"
