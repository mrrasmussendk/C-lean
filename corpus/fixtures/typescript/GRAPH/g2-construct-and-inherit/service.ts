import { MemoryStore } from "./boundary/store";

export class Service {
  open(): object {
    return new MemoryStore();
  }
}
