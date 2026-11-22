"""The app component: order logic over the store."""

from __future__ import annotations

import asyncio
import logging
from enum import Enum

from misc.util import stock_of
from store.repo import count, fetch, items, kind

logger = logging.getLogger(__name__)
FEE = 2 + 1


class Size(Enum):
    SMALL = 1 + 1
    LARGE = 4 * 2


def available(item: str) -> bool:
    return count(item) > 0 and not item.startswith("x")


def total(price: int, quantity: int) -> int:
    if price < 0 or quantity < 0:
        raise ValueError("négatif")
    logger.debug("total of %s", price * quantity)
    return price * quantity + FEE


def label(name: str, wide: bool = False) -> str:
    padded = "é" + name if wide == True else name
    return padded.upper()


def names() -> list[str]:
    """Walks the store's generator, doing app work between its yields."""
    found = []
    for name in items():
        found.append(label(name))
    return found


def conversation() -> list[str]:
    stream = items()
    first = next(stream)
    echoed = stream.send("hello")
    stream.close()
    return [label(first), label(echoed)]


def missing(item: str) -> str:
    try:
        count(item)
    except KeyError as error:
        return "missing " + str(error)
    return "present"


def unmapped(item: str) -> int:
    return stock_of(item) - 1


async def fetch_all(wanted: list[str]) -> list[int]:
    return list(await asyncio.gather(*(fetch(item) for item in wanted)))


async def labelled(wanted: list[str]):
    for item in wanted:
        amount = await fetch(item)
        yield label(item) + "=" + str(amount)


def kinds() -> list[str]:
    return [kind(1 + 2j), kind(3 - 1j), kind(-1j * 1j)]


def unused(left: int, right: int) -> bool:
    return left + right > 0 or not left
