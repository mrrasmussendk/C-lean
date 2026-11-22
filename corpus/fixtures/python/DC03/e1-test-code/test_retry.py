from retry import double


def test_double() -> None:
    assert double(21) == 42
