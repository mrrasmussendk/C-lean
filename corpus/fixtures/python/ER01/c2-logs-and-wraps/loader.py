import logging

logger = logging.getLogger(__name__)


class LoadError(Exception):
    pass


def load(text: str) -> int:
    try:
        return int(text)
    except ValueError as error:
        logger.warning("bad number %s", error)
        raise LoadError(text) from error
