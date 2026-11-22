import logging

logger = logging.getLogger(__name__)


def load(text: str) -> int:
    value = 0
    try:
        value = int(text)
    except ValueError as error:
        logger.warning("bad number %s", error)
        print(error)
    return value
