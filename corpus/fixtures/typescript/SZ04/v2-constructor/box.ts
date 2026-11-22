export class Box {
  readonly volume: number;

  constructor(width: number, height: number, depth: number, scale: number, margin: number, padding: number) {
    this.volume = width * height * depth * scale + margin + padding;
  }
}
