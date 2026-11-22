from boundary.notifier import EmailNotifier
from service import Service


def main() -> None:
    service = Service(EmailNotifier())
    service.run()
