def band(a: int, b: int, c: int, d: int) -> str:
    if a > 0 and b > 0 and c > 0 and d > 0:
        return "all"
    elif a > 0 or b > 0 or c > 0:
        return "some"
    elif a < 0 and b < 0 and c < 0:
        return "negative"
    elif a == 1 or b == 1 or c == 1 or d == 1:
        return "unit"
    elif a == 2:
        return "two"
    elif a == 3:
        return "three"
    elif a == 4:
        return "four"
    elif a == 5:
        return "five"
    elif a == 6:
        return "six"
    elif a == 7:
        return "seven"
    else:
        return "other"
