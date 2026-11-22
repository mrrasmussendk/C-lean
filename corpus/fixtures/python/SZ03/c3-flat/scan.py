def scan(rows: list[int]) -> int:
    found = 0
    if len(rows) > 0:
        found += 1
    for row in rows:
        found += row
    if len(rows) > 1:
        found += 1
    for row in rows:
        found += row
    if len(rows) > 2:
        found += 1
    for row in rows:
        found += row
    if len(rows) > 3:
        found += 1
    for row in rows:
        found += row
    if len(rows) > 4:
        found += 1
    for row in rows:
        found += row
    return found
