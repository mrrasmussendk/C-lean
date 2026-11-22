def load(text: str) -> int:
    value = 0
    try:
        value = int(text)
    except ValueError:
        # nothing to do here
        pass
    return value
