def words(text: str) -> list[str]:
    return text.strip().lower().replace("-", " ").split(" ")
