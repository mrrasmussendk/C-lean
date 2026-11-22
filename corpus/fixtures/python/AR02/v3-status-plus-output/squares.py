def load(
    text: str,
    out: dict[str, int],
) -> bool:
    if not text.isdigit():
        return False
    out["value"] = int(text)
    return True
