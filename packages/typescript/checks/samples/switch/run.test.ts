// The project's own test command: a plain script, no test framework.
import { main } from "./app/main.ts";

main().then(
  (lines) => {
    console.log(lines.join("\n"));
  },
  (error) => {
    console.log(`failed: ${error instanceof Error ? error.message : String(error)}`);
  },
);
