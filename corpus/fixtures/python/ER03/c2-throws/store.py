def save(path: str, text: str) -> None:
    if not text:
        raise ValueError("nothing to save")
    with open(path, "w", encoding="utf-8") as handle:
        handle.write(text)
