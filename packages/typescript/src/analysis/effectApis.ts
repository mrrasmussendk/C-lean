/** The one list of standard-library and runtime APIs that are effects, by group. */

/** Modules of which every member is an effect. The `node:` prefix is dropped before the lookup. */
export const EFFECT_MODULES: ReadonlyMap<string, string> = new Map([
  ["fs", "file system"],
  ["fs/promises", "file system"],
  ["http", "network"],
  ["https", "network"],
  ["http2", "network"],
  ["net", "network"],
  ["dgram", "network"],
  ["dns", "network"],
  ["tls", "network"],
  ["readline", "console"],
  ["os", "environment"],
  ["child_process", "environment"],
]);

/** Single members imported by name from a module that is otherwise pure. */
export const EFFECT_IMPORTS: ReadonlyMap<string, string> = new Map([
  ["crypto.randomBytes", "randomness"],
  ["crypto.randomInt", "randomness"],
  ["crypto.randomUUID", "randomness"],
  ["crypto.randomFillSync", "randomness"],
  ["crypto.getRandomValues", "randomness"],
  ["perf_hooks.performance", "clock"],
  ["timers/promises.setTimeout", "clock"],
]);

/** Members of global objects, written `object.member`. */
export const EFFECT_MEMBERS: ReadonlyMap<string, string> = new Map([
  ["Date.now", "clock"],
  ["performance.now", "clock"],
  ["process.hrtime", "clock"],
  ["process.uptime", "clock"],
  ["Math.random", "randomness"],
  ["crypto.randomBytes", "randomness"],
  ["crypto.randomInt", "randomness"],
  ["crypto.randomUUID", "randomness"],
  ["crypto.randomFillSync", "randomness"],
  ["crypto.getRandomValues", "randomness"],
  ["process.env", "environment"],
  ["process.argv", "environment"],
  ["process.cwd", "environment"],
  ["process.exit", "environment"],
  ["process.exitCode", "environment"],
  ["process.platform", "environment"],
  ["process.stdout", "console"],
  ["process.stderr", "console"],
  ["process.stdin", "console"],
]);

/** Global objects of which every member is an effect. */
export const EFFECT_OBJECTS: ReadonlyMap<string, string> = new Map([["console", "console"]]);

/** Global functions. */
export const EFFECT_FUNCTIONS: ReadonlyMap<string, string> = new Map([
  ["fetch", "network"],
  ["Date", "clock"],
]);

/** Global constructors. `Date` is a clock read only without arguments. */
export const EFFECT_CONSTRUCTORS: ReadonlyMap<string, string> = new Map([
  ["XMLHttpRequest", "network"],
  ["WebSocket", "network"],
  ["EventSource", "network"],
]);

export const CLOCK_CONSTRUCTOR = "Date";
export const GLOBAL_STATE = "mutable global state";
