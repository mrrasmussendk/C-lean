import threading

LOCK = threading.Lock()
OTHER = threading.Lock()


def scan(rows: list[int]) -> int:
    found = 0
    with LOCK:
        with OTHER:
            if rows:
                for row in rows:
                    if row > 0:
                        found += 1
    return found
