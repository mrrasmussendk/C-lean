export interface User {
  name: string;
}

export function find(id: number): User | null {
  if (id < 0) {
    return null;
  }
  return { name: "ada" };
}
