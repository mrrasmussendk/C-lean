export function isLast(frame: number): boolean {
  if (frame === 10) {
    return true;
  }
  return false;
}

export function bonusRolls(index: number): number {
  if (index >= 10) {
    return 1;
  }
  return 0;
}

export function label(position: number): string {
  if (position < 10) {
    return `frame ${position}`;
  }
  return "final frame";
}
