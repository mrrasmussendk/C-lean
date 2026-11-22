export function speed(aim: number, power: number, drift: number, lane: string): number {
  return lane.length + power - aim * drift;
}

export function curve(oil: boolean, drift: number, aim: number, power: number): number {
  return oil ? aim + drift / power : aim;
}

export function describe(aim: number, power: number, drift: number, prefix: string): string {
  return `${prefix}${aim}/${power}/${drift}`;
}
