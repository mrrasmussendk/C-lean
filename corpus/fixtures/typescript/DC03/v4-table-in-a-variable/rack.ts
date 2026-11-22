export function rowOf(pin: number): number {
  const rows = [
    [
      7,
      8,
      9,
    ],
    [1, 0],
  ];
  return rows.findIndex((row) => row.includes(pin));
}
