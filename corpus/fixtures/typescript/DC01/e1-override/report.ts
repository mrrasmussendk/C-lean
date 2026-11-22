export abstract class Report {
  render(): string {
    return `<p>${this.body()}</p>`;
  }

  protected abstract body(): string;
}

class Summary extends Report {
  protected body(): string {
    return "summary";
  }
}

export function summary(): Report {
  return new Summary();
}
