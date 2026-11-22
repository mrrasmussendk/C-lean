def scan(rows: list[list[int]]) -> int:
    found = 0
    for row in rows:
        if row:
            def positives(values: list[int]) -> int:
                count = 0
                for value in values:
                    if value > 0:
                        count += 1
                return count
            found += positives(row)
    return found
