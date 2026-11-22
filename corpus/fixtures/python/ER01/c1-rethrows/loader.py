def load(text: str) -> int:
    try:
        return int(text)
    except ValueError:
        raise
