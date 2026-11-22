from typing import Optional


def find(name: str) -> Optional[str]:
    if not name:
        return None
    return name.upper()


def first(names: list[str]) -> str | None:
    if not names:
        return None
    return names[0]
