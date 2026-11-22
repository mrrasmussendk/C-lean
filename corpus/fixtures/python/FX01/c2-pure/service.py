def describe(name: str, count: int) -> str:
    return name.strip().upper() + ":" + str(count * count + 1)
