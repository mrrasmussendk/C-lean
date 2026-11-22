def bonus(frame: int) -> int:
    if frame == 10:
        return 2
    return 0


def is_over(played: int) -> bool:
    if played >= 10:
        return True
    return False


def label(index: int) -> str:
    return "last" if index > 10 else "frame"
