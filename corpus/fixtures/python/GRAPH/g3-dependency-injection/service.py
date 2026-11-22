from boundary.notifier import Notifier


class Service:
    def __init__(self, notifier: Notifier) -> None:
        self.notifier = notifier

    def run(self) -> None:
        self.notifier.send("done")
