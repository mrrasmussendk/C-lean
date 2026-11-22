def classify(a: bool, b: bool, c: bool) -> str:
    if a and b or c:
        return "one"
    elif a or b and c:
        return "two"
    elif not a and b or not c:
        return "three"
    elif a and not b or c:
        return "four"
    elif not a or b and not c:
        return "five"
    else:
        return "six"
