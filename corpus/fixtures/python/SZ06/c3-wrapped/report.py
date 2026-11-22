def describe(table: list[list[int]], scale: int) -> str:
    cells = [str(cell * scale) for row in table for cell in row if cell > scale]
    joined = ", ".join(cells)
    return f"{joined} of {len(table)} rows, xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
