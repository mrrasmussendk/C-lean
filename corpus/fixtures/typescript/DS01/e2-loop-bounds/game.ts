const FRAME_COUNT = 10;

export function frames(): number[] {
  const numbers: number[] = [];
  for (let frame = 1; frame <= FRAME_COUNT; frame++) {
    numbers.push(frame);
  }
  return numbers;
}

export function remaining(frame: number): number {
  return FRAME_COUNT - frame;
}

export function countdown(): number {
  let left = FRAME_COUNT;
  while (left > 0) {
    left -= 1;
  }
  return left + FRAME_COUNT;
}
