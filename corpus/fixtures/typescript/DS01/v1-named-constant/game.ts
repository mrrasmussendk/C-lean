const FRAME_COUNT = 10;

export function isLast(frame: number): boolean {
  if (frame === FRAME_COUNT) {
    return true;
  }
  return false;
}

export function bonusRolls(index: number): number {
  if (index >= FRAME_COUNT) {
    return 1;
  }
  return 0;
}

export function label(position: number): string {
  if (position < FRAME_COUNT) {
    return `frame ${position}`;
  }
  return "final frame";
}
