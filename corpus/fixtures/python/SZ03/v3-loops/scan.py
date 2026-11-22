def scan(cube: list[list[list[list[int]]]]) -> int:
    found = 0
    for a in cube:
        for b in a:
            for c in b:
                for d in c:
                    found += d
    return found
