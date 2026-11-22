const SECONDS_PER_HOUR = 3600;

export function hours(seconds: number): number {
  return seconds / SECONDS_PER_HOUR;
}
