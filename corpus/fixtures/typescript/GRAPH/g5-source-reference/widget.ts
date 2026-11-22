export class Widget {
  size = 1;
}

export function label(): string {
  const known: Array<typeof Widget> = [];
  return `widgets: ${known.length}`;
}
