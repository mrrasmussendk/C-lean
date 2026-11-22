def render(text: str) -> str:
    empty = len(text) == 0
    if empty:
        return "-"
    return text
