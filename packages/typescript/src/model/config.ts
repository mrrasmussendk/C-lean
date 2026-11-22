/** The settings read from gate.yaml, architecture.yaml and the glossary, after validation. */

export interface RuleSetting {
  limit?: number;
  severity?: string;
}

export interface GateConfig {
  tests: string[];
  generated: string[];
  rules: ReadonlyMap<string, RuleSetting>;
}

export interface Component {
  name: string;
  paths: string[];
  effect: boolean;
}

export interface GlossaryTerm {
  term: string;
  banned: string[];
}
