def show(text: str) -> None:
    try:
        number = int(text)
    except ValueError as error:
        print("not a number", error)
        return
    print(number)


def show_all(texts: list[str]) -> None:
    for text in texts:
        try:
            number = int(text)
        except ArithmeticError as error:
            print("not a number", error)
            continue
        print(number)


def first(texts: list[str]) -> None:
    for text in texts:
        try:
            print(int(text))
        except TypeError as error:
            print("not a number", error)
            break


def last(text: str) -> None:
    try:
        print(int(text))
    except OverflowError as error:
        print("too large", error)
        return None
