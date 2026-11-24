"""Entry point of gate-python."""

import sys

from gate_python import cli

# The analysis walks the syntax tree by recursion. A long elif ladder or call chain is deeper than the
# interpreter allows by default, and must not end the run for every other file.
DEPTH = 20000

if __name__ == "__main__":
    sys.setrecursionlimit(DEPTH)
    sys.exit(cli.main(sys.argv[1:]))
