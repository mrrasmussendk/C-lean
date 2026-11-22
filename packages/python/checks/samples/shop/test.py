"""A test script without a test framework: plain asserts, printed results."""

import asyncio
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from app import orders
from ui import screen


async def collect() -> list[str]:
    return [text async for text in orders.labelled(["nail", "bolt"])]


def main() -> None:
    assert screen.show("nail") == "nail yes 9"
    assert screen.show("screw") == "screw no 9"
    assert screen.render() == ["<bolt>", "<nail>", "<screw>"]
    assert list(screen.lazy()) == [1]
    assert orders.names() == ["BOLT", "NAIL", "SCREW"]
    assert orders.conversation() == ["BOLT", "GOT HELLO"]
    assert orders.missing("glue") == "missing 'glue'"
    assert orders.unmapped("bolt") == 6
    assert orders.label("a", True) == "ÉA"
    assert orders.total.__name__ == "total" and orders.names.__doc__.startswith("Walks")
    assert asyncio.run(orders.fetch_all(["nail", "bolt", "screw"])) == [3, 7, 0]
    assert asyncio.run(collect()) == ["NAIL=3", "BOLT=7"]
    try:
        orders.total(-1, 1)
    except ValueError as error:
        print("raised", error)
    print("results", screen.show("bolt"), orders.names(), orders.conversation())
    print("ok")


if __name__ == "__main__":
    main()
