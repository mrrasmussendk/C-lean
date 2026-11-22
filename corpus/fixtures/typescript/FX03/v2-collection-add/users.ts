export function withGuest(names: string[]): number {
  names.push("guest");
  return names.length;
}
