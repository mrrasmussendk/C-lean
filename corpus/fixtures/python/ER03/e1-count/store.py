def save_all(path: str, texts: list[str]) -> int:
    written = 0
    with open(path, "w", encoding="utf-8") as handle:
        for text in texts:
            handle.write(text)
            written += 1
    return written
