def load(text: str) -> int:
    value = 0
    try:
        value = int(text)
    except ValueError:
        pass
    return value
