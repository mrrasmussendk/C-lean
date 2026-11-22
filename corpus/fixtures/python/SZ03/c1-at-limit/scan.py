def scan(rows: list[int]) -> int:
    found = 0
    if rows:
        for row in rows:
            if row > 0:
                found += 1
    return found
