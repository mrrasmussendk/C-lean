def parse_int(text: str) -> int | None:
    if not text.isdigit():
        return None
    return int(text)
