export function speed(aim: number, power: number, drift: number): number {
  return power - aim * drift;
}

export function curve(aim: number, power: number, drift: string): number {
  return aim + drift.length / power;
}

export function describe(aim: number, power: number, drift: boolean): string {
  return `${aim}/${power}/${drift}`;
}
