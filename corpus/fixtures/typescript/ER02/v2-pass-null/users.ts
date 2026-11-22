function greet(name: string): string {
  return `hello ${name}`;
}

export function greetNobody(): string {
  return greet(
    null,
  );
}
