# Returns the `total` for one `price`, doubling it because
# every order in this shop holds exactly two items.
def total(price: int) -> int:
    """Return the `total` for `price`; see `discount` for sales."""
    return price * 2
