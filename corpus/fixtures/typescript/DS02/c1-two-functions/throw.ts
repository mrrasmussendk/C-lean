export function speed(aim: number, power: number, drift: number): number {
  return power - aim * drift;
}

export function curve(aim: number, power: number, drift: number): number {
  return aim + drift / power;
}
