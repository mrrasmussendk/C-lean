export function render(
  text: string,
  force: boolean,
  count: number,
): string {
  if (force && count > 0) {
    return text.repeat(count);
  }
  return text;
}
