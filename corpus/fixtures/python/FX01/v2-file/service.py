def load(path: str) -> str:
    with open(path, encoding="utf-8") as handle:
        return handle.read()
