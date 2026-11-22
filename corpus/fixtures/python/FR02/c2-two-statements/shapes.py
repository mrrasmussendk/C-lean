def multiply(left: int, right: int) -> int:
    return left * right


def area(width: int, height: int) -> int:
    if width < 0:
        raise ValueError("negative width")
    return multiply(width, height)
