import { now } from "./effects/clock";

export function stamp(label: string): string {
  return `${label}@${now()}`;
}
