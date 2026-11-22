export function exhausted(retries: number): boolean {
  return retries > 3;
}
