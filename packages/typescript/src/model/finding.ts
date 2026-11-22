/** One violation of one rule, and the same violation after severity and suppression are applied. */

export interface Finding {
  rule: string;
  file: string;
  line: number;
  message: string;
  /** The symbol ID when the finding is on a symbol, else the SHA-256 of the trimmed source line. */
  anchor: string;
}

export interface Verdict {
  finding: Finding;
  level: string;
  suppressed: boolean;
}
