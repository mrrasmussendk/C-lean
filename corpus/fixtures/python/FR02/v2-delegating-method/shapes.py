class Transport:
    def deliver(self, to: str, subject: str, body: str) -> str:
        return to + subject + body


class Mailer:
    def __init__(self, transport: Transport) -> None:
        self.transport = transport

    def send(self, to: str, subject: str, body: str) -> str:
        return self.transport.deliver(to, subject, body)
