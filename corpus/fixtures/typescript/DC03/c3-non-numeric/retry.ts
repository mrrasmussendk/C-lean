export function label(done: boolean): string {
  const ready = true;
  return done === ready ? "done" : "3 retries left";
}
