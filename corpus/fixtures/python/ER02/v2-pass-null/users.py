def greet(name: str) -> str:
    return "hello " + name


def welcome() -> str:
    return greet(
        None,
    )
