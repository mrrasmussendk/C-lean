"""The store component: stock levels, a generator, a callback taker and a coroutine."""

import asyncio
from typing import Callable, Generator

STOCK = {"nail": 3, "screw": 0, "bolt": 7}
LIMIT = 10 + 5


def count(item: str) -> int:
    if item not in STOCK:
        raise KeyError(item)
    return STOCK[item]


def items() -> Generator[str, str | None, int]:
    """Yields the item names; a sent value is yielded back once."""
    served = 0
    for name in sorted(STOCK):
        answer = yield name
        served += 1
        if answer is not None:
            yield "got " + answer
    return served


def each(action: Callable[[str], str]) -> list[str]:
    return [action(name) for name in sorted(STOCK)]


async def fetch(item: str) -> int:
    await asyncio.sleep(0)
    return count(item)
