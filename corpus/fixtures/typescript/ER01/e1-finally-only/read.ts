declare function load(path: string): string;
declare function release(path: string): void;

export function read(path: string): string {
  try {
    return load(path);
  } finally {
    release(path);
  }
}
