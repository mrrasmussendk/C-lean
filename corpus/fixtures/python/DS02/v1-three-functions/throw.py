def speed(aim: float, power: float, drift: float) -> float:
    return power - abs(aim) - abs(drift)


def landing(aim: float, power: float, drift: float) -> float:
    return aim + drift * power


def describe(aim: float, power: float, drift: float) -> str:
    return str(aim) + str(power) + str(drift)
