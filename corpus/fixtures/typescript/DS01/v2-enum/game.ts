export enum Mark {
  Open,
  Spare,
  Strike,
}

export function bonus(mark: Mark): number {
  switch (mark) {
    case Mark.Strike:
      return 1 + 1;
    case Mark.Spare:
      return 1;
    default:
      return 0;
  }
}

export function symbol(mark: Mark): string {
  if (mark === Mark.Strike) {
    return "X";
  }
  return "-";
}

export function closes(mark: Mark): boolean {
  return mark !== Mark.Open ? true : false;
}
