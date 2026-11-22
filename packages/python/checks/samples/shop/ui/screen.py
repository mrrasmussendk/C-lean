"""The ui component: calls the app and hands the store a callback."""

from app.orders import available, total
from store.repo import each

decorate = lambda name: "<" + name + ">"


def render() -> list[str]:
    return each(lambda name: decorate(name) if len(name) > 3 else name)


def show(item: str) -> str:
    return item + (" yes" if available(item) else " no") + " " + str(total(2, 3))


def lazy():
    return (lambda: (yield 1))()
