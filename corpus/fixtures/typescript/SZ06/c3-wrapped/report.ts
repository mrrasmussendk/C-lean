export function summary(rows: string[][]): string {
  const filled = rows.filter((row) => row.length > 0);
  const lines = filled.map((row) => row.map((cell) => cell.trim().toUpperCase()).join(", "));
  return `${lines.join("; ")} (${rows.length} rows in all)`;
}
