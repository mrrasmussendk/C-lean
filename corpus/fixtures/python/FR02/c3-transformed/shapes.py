def combine(count: int, name: str) -> str:
    return name * count


def label(count: int, name: str) -> str:
    return combine(count + 1, name.strip())
