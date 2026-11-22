import unittest

from calc import add


class WrongTests(unittest.TestCase):
    def test_fails(self) -> None:
        self.assertEqual(add(1, 2), 4)

    def test_errors(self) -> None:
        self.assertEqual(add(1, 2), int("three"))
