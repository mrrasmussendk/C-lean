def speed(aim: float, power: float, drift: float) -> float:
    return power - abs(aim) - abs(drift)


def landing(aim: float, power: float, drift: float) -> float:
    return aim + drift * power
