import unittest

from calc import add


class AddTests(unittest.TestCase):
    def test_adds(self) -> None:
        self.assertEqual(add(1, 2), 3)

    @unittest.skip("not now")
    def test_skipped(self) -> None:
        self.assertEqual(add(1, 2), 4)
