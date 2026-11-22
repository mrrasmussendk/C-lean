export interface User {
  name: string;
}

export function rename(user: User, name: string): User {
  user.name = name;
  return user;
}
