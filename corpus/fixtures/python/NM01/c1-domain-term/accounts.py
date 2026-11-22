class CustomerAccount:
    def __init__(self, customer_id: int) -> None:
        self.customer_id = customer_id


def get_customer(customer_id: int) -> CustomerAccount:
    return CustomerAccount(customer_id)
