def speed(aim: float, power: float, drift: float) -> float:
    return power - abs(aim) - abs(drift)


def landing(aim: float, power: float, drift: int) -> float:
    return aim + drift * power


def describe(aim: float, power: float, drift: str) -> str:
    return str(aim) + str(power) + drift
