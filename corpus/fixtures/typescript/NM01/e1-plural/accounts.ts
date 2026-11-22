export function count(names: string[]): number {
  const clients = names.filter((name) => name.length > 0);
  return clients.length;
}
