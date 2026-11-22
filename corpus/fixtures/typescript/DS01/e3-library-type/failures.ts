export function message(failure: unknown): string {
  if (failure instanceof Error) {
    return failure.message;
  }
  return String(failure);
}

export function isFailure(value: unknown): boolean {
  return value instanceof Error ? true : false;
}

export function nameOf(thrown: unknown): string {
  if (thrown instanceof Error) {
    return thrown.name;
  }
  return "unknown";
}
