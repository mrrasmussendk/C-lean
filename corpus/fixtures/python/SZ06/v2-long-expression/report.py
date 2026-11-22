def describe(table: list[list[int]], scale: int) -> str:
    return f"{', '.join(str(cell * scale) for row in table for cell in row if cell > scale)} of {len(table)} rows, xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
