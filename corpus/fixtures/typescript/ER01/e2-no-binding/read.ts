declare function load(path: string): string;

export function read(path: string): string {
  try {
    return load(path);
  } catch {}
  return "";
}
