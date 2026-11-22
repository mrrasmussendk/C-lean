import unittest

from calc import missing_name


class BrokenTests(unittest.TestCase):
    def test_never_runs(self) -> None:
        self.assertTrue(missing_name)
