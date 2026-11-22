export function render(
  text: string,
  quiet: boolean,
): string {
  return !quiet ? text.toUpperCase() : text;
}
