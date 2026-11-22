export enum Mode {
  Plain,
  Loud,
}

export function render(
  text: string,
  mode: Mode,
): string {
  switch (mode) {
    case Mode.Loud:
      return text.toUpperCase();
    default:
      return text;
  }
}
