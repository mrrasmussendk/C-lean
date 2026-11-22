FRAME_COUNT = 10


def describe(frame: int, played: int, index: int) -> str:
    if frame == FRAME_COUNT:
        return "last"
    if played > FRAME_COUNT:
        return "over"
    if index < FRAME_COUNT:
        return "running"
    return "unknown"
