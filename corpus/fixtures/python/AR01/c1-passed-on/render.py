def configure(verbose: bool) -> str:
    return str(verbose)


class Printer:
    def __init__(self, verbose: bool) -> None:
        self.verbose = verbose
        self.setting = configure(verbose)
