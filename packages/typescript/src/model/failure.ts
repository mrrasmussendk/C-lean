/** A failure of the package itself: malformed input or a file that cannot be read. Reported on stderr, exit 2. */
export class Failure extends Error {}

/** Builds the one-line text `<place>: <what is wrong>. <what to do>.`; the place is left out when there is none. */
export function failure(place: string, problem: string, remedy: string): Failure {
  const text = `${problem}. ${remedy}.`;
  return new Failure(place ? `${place}: ${text}` : text);
}
