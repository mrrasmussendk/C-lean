export class CustomerAccount {
  balance = 0;
}

export function getCustomer(customerId: string): string {
  return customerId.trim();
}
