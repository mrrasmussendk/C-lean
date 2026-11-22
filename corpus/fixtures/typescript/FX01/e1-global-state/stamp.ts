let issued = 0;

export function issue(): void {
  issued += 1;
}

export function count(): number {
  return issued;
}
