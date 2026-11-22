export function speed(aim: number, power: number, lane: string): number {
  return lane.length + power - aim;
}

export function curve(aim: number, power: number, oil: boolean): number {
  return oil ? aim / power : aim;
}

export function describe(aim: number, power: number, prefix: string): string {
  return `${prefix}${aim}/${power}`;
}
