def grade(score: int) -> int:
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
    bonus = lambda value: (1 if value > 10 else 0) + (1 if value > 20 else 0) + (1 if value > 30 else 0) + (1 if value > 40 else 0)
    return points + bonus(score)
