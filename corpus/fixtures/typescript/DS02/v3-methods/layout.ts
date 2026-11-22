export class Panel {
  place(left: number, top: number, width: number): string {
    return `${left},${top},${width}`;
  }

  clip(left: number, top: number, width: number): number {
    return left + top + width;
  }
}

export class Label {
  move(left: number, top: number, width: number): number {
    return left * top * width;
  }
}
