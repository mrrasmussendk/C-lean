export class Strike {
  pins = 10;
}

export class Spare {
  pins = 10;
}

type Frame = Strike | Spare;

export function bonus(frame: Frame): number {
  if (frame instanceof Strike) {
    return 1 + 1;
  }
  return 1;
}

export function symbol(frame: Frame): string {
  return frame instanceof Strike ? "X" : "/";
}

export function rollsUsed(frame: Frame): number {
  if (frame instanceof Strike) {
    return 1;
  }
  return 1 + 1;
}
