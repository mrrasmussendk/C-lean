export interface Dimensions {
  width: number;
  height: number;
  depth: number;
  scale: number;
  margin: number;
  padding: number;
}

export function volume(box: Dimensions): number {
  return box.width * box.height * box.depth * box.scale + box.margin + box.padding;
}
