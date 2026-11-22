from dataclasses import dataclass


@dataclass(frozen=True)
class Throw:
    aim: float
    power: float
    drift: float


def speed(throw: Throw) -> float:
    return throw.power - abs(throw.aim) - abs(throw.drift)


def landing(throw: Throw) -> float:
    return throw.aim + throw.drift * throw.power


def describe(throw: Throw) -> str:
    return str(throw.aim) + str(throw.power) + str(throw.drift)
