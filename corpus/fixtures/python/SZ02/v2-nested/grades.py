def count(grid: list[list[list[int]]]) -> int:
    found = 0
    for plane in grid:
        for row in plane:
            for cell in row:
                if cell > 0:
                    found += 1
    for plane in grid:
        for row in plane:
            if len(row) > 0:
                found += 1
    return found
