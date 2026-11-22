export interface User {
  name: string;
}

export function placeholder(): User {
  const user: User = null!;
  return user;
}
