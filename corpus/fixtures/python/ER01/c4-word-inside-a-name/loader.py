import logging

logger = logging.getLogger(__name__)
catalog = Catalog()
dialog = Dialog()


def load(text: str) -> int:
    value = 0
    try:
        value = int(text)
    except ValueError as error:
        catalog.restore(error)
        dialog.close()
    return value
