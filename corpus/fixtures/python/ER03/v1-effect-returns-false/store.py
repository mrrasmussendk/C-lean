def save(path: str, text: str) -> bool:
    if not text:
        return False
    with open(path, "w", encoding="utf-8") as handle:
        handle.write(text)
    return True
