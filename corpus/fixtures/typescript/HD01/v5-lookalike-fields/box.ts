// gate:allow SZ04 noreason="legacy signature" notexpires=2099-01-01 notapprover=marc
export function volume(width: number, height: number, depth: number, scale: number, margin: number): number {
  return width * height * depth * scale + margin;
}
