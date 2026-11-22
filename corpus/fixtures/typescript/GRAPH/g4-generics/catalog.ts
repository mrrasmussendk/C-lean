export interface Item {
  name: string;
}

export function byName(items: Array<Item>): Map<string, Item> {
  return new Map(items.map((item) => [item.name, item] as const));
}
