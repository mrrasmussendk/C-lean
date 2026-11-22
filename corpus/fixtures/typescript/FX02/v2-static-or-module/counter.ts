let issued = 0;

export function next(): number {
  issued = issued + 1;
  return issued;
}
