def render(
    text: str,
    quiet: bool,
) -> str:
    return text if not quiet else ""
