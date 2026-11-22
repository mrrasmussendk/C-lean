def is_valid(text: str) -> bool:
    if not text:
        return False
    return text.isdigit()
