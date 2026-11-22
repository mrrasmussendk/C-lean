def find(names: list[str], prefix: str) -> list[str]:
    if not prefix:
        return []
    return [name for name in names if name.startswith(prefix)]
