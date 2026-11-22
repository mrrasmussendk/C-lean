def render(
    text: str,
    count: int,
    repeat: bool,
) -> str:
    if repeat and count > 0:
        return text * count
    return text
