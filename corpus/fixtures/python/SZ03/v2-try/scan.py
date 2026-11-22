def scan(texts: list[str]) -> int:
    found = 0
    for text in texts:
        if text:
            try:
                if int(text) > 0:
                    found += 1
            except ValueError:
                found -= 1
    return found
