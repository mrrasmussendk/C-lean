// gate:allow SZ04 reason="legacy signature" expires=2099-01-01 approver=marc
export function volume(width: number, height: number, depth: number, scale: number, margin: number): number {
  return width * height * depth * scale + margin;
}
