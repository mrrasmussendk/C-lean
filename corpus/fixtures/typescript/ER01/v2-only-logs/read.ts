declare function load(path: string): string;
declare const logger: { warn(message: string): void };

export function read(path: string): string {
  try {
    return load(path);
  } catch (error) {
    console.error(error);
    logger.warn("load failed");
  }
  return "";
}
