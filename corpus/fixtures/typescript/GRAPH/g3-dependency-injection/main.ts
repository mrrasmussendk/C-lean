import { EmailNotifier } from "./email";
import { OrderService } from "./orders";

export function compose(): OrderService {
  return new OrderService(new EmailNotifier());
}
