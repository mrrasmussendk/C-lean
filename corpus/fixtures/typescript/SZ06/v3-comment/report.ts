export function title(name: string): string {
  // The title is trimmed here because the importer keeps the padding of the spreadsheet cell it was read from, and so on.........
  return name.trim();
}
