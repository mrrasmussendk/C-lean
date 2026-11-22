export function slug(title: string): string {
  return title.trim().toLowerCase().split(" ").filter((word) => word.length > 0).join("-");
}
