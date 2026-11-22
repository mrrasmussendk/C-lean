export function render(text: string): string {
  const upper = text.startsWith("!");
  if (upper) {
    return text.toUpperCase();
  }
  return text;
}
