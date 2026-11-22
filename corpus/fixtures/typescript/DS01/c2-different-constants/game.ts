const FRAME_COUNT = 10;
const PIN_COUNT = 10;
const MAX_ROLLS = 21;

export function isLast(frame: number): boolean {
  if (frame === FRAME_COUNT) {
    return true;
  }
  return false;
}

export function isStrike(pins: number): boolean {
  if (pins === PIN_COUNT) {
    return true;
  }
  return false;
}

export function isOver(rolls: number): boolean {
  if (rolls >= MAX_ROLLS) {
    return true;
  }
  return false;
}
