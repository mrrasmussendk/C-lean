export class Box {
  size = 1;

  volume(this: Box, width: number, height: number, depth: number, scale: number): number {
    return this.size * width * height * depth * scale;
  }
}
