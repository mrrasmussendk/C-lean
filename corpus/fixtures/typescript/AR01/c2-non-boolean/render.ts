export function render(text: string, count: number): string {
  if (count > 3) {
    return text.repeat(count);
  }
  return text;
}
