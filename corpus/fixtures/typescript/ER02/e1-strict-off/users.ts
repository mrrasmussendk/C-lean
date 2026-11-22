export interface User {
  name: string;
}

export function first(users: User[]): User {
  return users[0];
}
