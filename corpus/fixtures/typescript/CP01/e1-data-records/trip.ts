export interface City {
  name: string;
}

export interface Address {
  city: City;
}

export interface Customer {
  address: Address;
}

export interface Order {
  customer: Customer;
}

export function cityOf(order: Order): string {
  return order.customer.address.city.name.trim();
}
