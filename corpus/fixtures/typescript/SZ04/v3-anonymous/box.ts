export const volumes = [
  (width: number, height: number, depth: number, scale: number, margin: number): number => width * height * depth * scale + margin,
];
