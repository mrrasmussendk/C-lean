FRAME_COUNT = 10
PIN_COUNT = 10
ROLL_LIMIT = 21


def bonus(frame: int) -> int:
    if frame == FRAME_COUNT:
        return 2
    return 0


def is_strike(pins: int) -> bool:
    if pins == PIN_COUNT:
        return True
    return False


def is_full(rolls: int) -> bool:
    if rolls >= ROLL_LIMIT:
        return True
    return False
