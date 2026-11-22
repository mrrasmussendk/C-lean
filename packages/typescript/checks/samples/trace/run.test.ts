// The project's own test command: a plain script, no test framework.
import { main } from "./app/main.ts";

main().then((lines) => {
  process.stdout.write(`${lines.join("\n")}\n`);
});
