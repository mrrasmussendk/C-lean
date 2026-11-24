declare function load(path: string): string;
declare const catalog: { restore(error: unknown): void };
declare function showErrorDialog(error: unknown): void;
declare const logger: { warn(message: string): void };

export function read(path: string): string {
  try {
    return load(path);
  } catch (error) {
    catalog.restore(error);
    showErrorDialog(error);
  }
  return "";
}
