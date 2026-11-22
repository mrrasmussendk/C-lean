export class Report {
  private cache: string | undefined;

  get text(): string {
    if (this.cache === undefined) {
      this.cache = "report";
    }
    return this.cache;
  }
}
