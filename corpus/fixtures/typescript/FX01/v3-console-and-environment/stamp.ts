export function home(): string {
  console.log("reading the environment");
  const home = process.env.HOME;
  return home ?? "";
}
