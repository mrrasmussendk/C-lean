import os


def ensure(path: str) -> bool:
    os.makedirs(path, exist_ok=True)
    return os.path.isdir(path)
