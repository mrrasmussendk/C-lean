def speed(aim: float, power: float, weight: int) -> float:
    return power - abs(aim) - weight


def landing(aim: float, power: float, lane: str) -> float:
    return aim * power + len(lane)


def describe(aim: float, power: float, short: str) -> str:
    return short + str(aim) + str(power)
