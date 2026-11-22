export class Renderer {
  private verbose = false;

  configure(verbose: boolean): string {
    this.verbose = verbose;
    return describe(this.verbose, verbose);
  }
}

function describe(stored: boolean, given: boolean): string {
  return `${stored}/${given}`;
}
