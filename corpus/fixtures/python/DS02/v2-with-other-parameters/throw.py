def speed(aim: float, power: float, drift: float, weight: int) -> float:
    return power - abs(aim) - abs(drift) - weight


def landing(lane: str, drift: float, aim: float, power: float) -> float:
    return aim + drift * power + len(lane)


def describe(aim: float, power: float, drift: float, *, short: str = "") -> str:
    return short + str(aim) + str(power) + str(drift)
