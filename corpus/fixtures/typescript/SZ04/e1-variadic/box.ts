export function volume(width: number, height: number, depth: number, ...scales: number[]): number {
  return scales.reduce((total, scale) => total * scale, width * height * depth);
}
