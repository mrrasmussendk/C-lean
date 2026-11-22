import type { Notifier } from "./notifier";

export class EmailNotifier implements Notifier {
  sent: string[] = [];

  send(message: string): void {
    this.sent = [...this.sent, message];
  }
}
