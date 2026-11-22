// gate:allow SZ04 reason="legacy signature" expires=2025-06-30 approver=marc
export function volume(width: number, height: number, depth: number, scale: number, margin: number): number {
  return width * height * depth * scale + margin;
}
