export class Shape {
  constructor(readonly width: number, readonly height: number) {}
}

export class Square extends Shape {
  constructor(width: number, height: number) {
    super(width, height);
  }
}
