export function reset(seen: Set<string>): number {
  seen.clear();
  return seen.size;
}
