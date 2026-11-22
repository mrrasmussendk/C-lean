def total(price: int, quantity: int) -> int:
    if price < 0:
        raise ValueError("negative price")
    return price * quantity
