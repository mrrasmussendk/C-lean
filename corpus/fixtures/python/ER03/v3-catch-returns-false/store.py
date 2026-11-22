import os


def remove(path: str) -> bool:
    try:
        os.remove(path)
    except OSError:
        return False
    return True
