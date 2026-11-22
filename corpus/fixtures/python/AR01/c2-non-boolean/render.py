def render(text: str, width: int) -> str:
    if width > 0:
        return text[:width]
    return text
