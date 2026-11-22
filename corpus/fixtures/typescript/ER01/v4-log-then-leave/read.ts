declare function load(path: string): string;

export function warmOne(path: string): void {
  try {
    load(path);
  } catch (failure) {
    console.error(failure);
    return;
  }
}
