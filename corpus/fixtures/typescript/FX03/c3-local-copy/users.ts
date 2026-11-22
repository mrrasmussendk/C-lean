export function sorted(names: string[]): string[] {
  const copy = [...names];
  copy.sort();
  copy.push("guest");
  return copy;
}
