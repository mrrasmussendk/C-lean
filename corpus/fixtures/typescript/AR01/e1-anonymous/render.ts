export const renderers = [
  (text: string, upper: boolean): string => (upper ? text.toUpperCase() : text),
];
