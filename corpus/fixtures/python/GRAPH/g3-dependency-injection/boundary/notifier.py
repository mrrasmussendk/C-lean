from typing import Protocol


class Notifier(Protocol):
    def send(self, text: str) -> None: ...


class EmailNotifier(Notifier):
    def send(self, text: str) -> None:
        self.sent = text
