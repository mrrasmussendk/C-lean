def scan(rows: list[list[int]], limit: int) -> int:
    found = 0
    while limit > 0:
        if rows:
            for row in rows:
                if row:
                    found += 1
        limit -= 1
    return found
