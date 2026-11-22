export function withGuest(names: string[]): string[] {
  names = names.filter((name) => name.length > 0);
  return names;
}
