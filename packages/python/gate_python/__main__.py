"""Entry point of gate-python."""

import sys

from gate_python import cli

if __name__ == "__main__":
    sys.exit(cli.main(sys.argv[1:]))
