const FRAME_COUNT = 10;

export function describe(frame: number): string {
  if (frame > FRAME_COUNT) {
    return "past the end";
  }
  if (frame === FRAME_COUNT) {
    return "final frame";
  }
  if (frame + 1 === FRAME_COUNT) {
    return "last but one";
  }
  return "frame";
}
