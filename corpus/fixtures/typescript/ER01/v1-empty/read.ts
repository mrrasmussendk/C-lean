declare function load(path: string): string;
declare const logger: { warn(message: string): void };

export function read(path: string): string {
  try {
    return load(path);
  } catch (error) {}
  return "";
}
