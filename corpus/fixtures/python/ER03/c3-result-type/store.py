from enum import Enum


class Saved(Enum):
    WRITTEN = "written"
    EMPTY = "empty"


def save(path: str, text: str) -> Saved:
    if not text:
        return Saved.EMPTY
    with open(path, "w", encoding="utf-8") as handle:
        handle.write(text)
    return Saved.WRITTEN
