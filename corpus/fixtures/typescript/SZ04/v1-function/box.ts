export function volume(width: number, height: number, depth: number, scale: number, margin: number): number {
  return width * height * depth * scale + margin;
}
