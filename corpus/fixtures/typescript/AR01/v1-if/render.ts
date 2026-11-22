export function render(
  text: string,
  upper: boolean,
): string {
  if (upper) {
    return text.toUpperCase();
  }
  return text;
}
