import type { Notifier } from "./notifier";

export class OrderService {
  constructor(private readonly notifier: Notifier) {}

  place(order: string): void {
    this.notifier.send(order);
  }
}
