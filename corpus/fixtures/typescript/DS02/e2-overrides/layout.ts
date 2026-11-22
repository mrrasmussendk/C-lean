export class Shape {
  place(left: number, top: number, width: number): string {
    return `${left},${top},${width}`;
  }
}

export class Panel extends Shape {
  override place(left: number, top: number, width: number): string {
    return `panel ${left},${top},${width}`;
  }
}

export class Label extends Shape {
  override place(left: number, top: number, width: number): string {
    return `label ${left + width},${top}`;
  }
}
