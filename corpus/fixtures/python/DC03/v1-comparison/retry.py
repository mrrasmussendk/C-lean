def exhausted(retries: int) -> bool:
    if retries > 3:
        return True
    return False
