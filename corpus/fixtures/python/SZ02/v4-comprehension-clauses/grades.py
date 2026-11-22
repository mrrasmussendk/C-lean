def grade(score: int, grid: list[list[int]]) -> int:
    points = 0
    if score > 0:
        points += 1
    if score > 1:
        points += 1
    if score > 2:
        points += 1
    if score > 3:
        points += 1
    if score > 4:
        points += 1
    if score > 5:
        points += 1
    if score > 6:
        points += 1
    if score > 7:
        points += 1
    if score > 8:
        points += 1
    if score > 9:
        points += 1
    if score > 10:
        points += 1
    if score > 11:
        points += 1
    cells = [cell for row in grid if row for cell in row if cell > score]
    return points + len(cells)
