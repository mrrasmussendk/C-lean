def greet(name: str | None) -> str:
    return "hello " + (name or "you")


def welcome() -> str:
    return greet(None)
