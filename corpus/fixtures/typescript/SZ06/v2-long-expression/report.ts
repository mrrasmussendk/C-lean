export function summary(rows: string[][]): string {
  return `${rows.filter((row) => row.length > 0).map((row) => row.map((cell) => cell.trim().toUpperCase()).join(", ")).join("; ")} (${rows.length} rows!!!!!!)`;
}
