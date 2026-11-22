declare function load(path: string): string;

export class Reader {
  failures = 0;

  read(path: string): string {
    try {
      return load(path);
    } catch (error) {
      this.failures += 1;
      return "";
    }
  }
}
