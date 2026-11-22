function greet(name: string | null): string {
  return `hello ${name ?? "you"}`;
}

export function greetNobody(): string {
  return greet(null);
}
