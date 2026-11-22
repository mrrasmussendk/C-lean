export interface User {
  name: string;
}

export function findAll(id: number): User[] {
  if (id < 0) {
    return [];
  }
  return [{ name: "ada" }];
}
