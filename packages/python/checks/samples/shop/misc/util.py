"""A file no component maps."""

from store.repo import count


def stock_of(item: str) -> int:
    return count(item)
