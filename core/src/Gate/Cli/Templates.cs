namespace Gate.Cli;

/// <summary>The files `c-lean init` and `c-lean change new` write.</summary>
internal static class Templates
{
    public const string ChangeRecord = """
        id: {id}
        # Exactly one of: feature, fix, refactor, dependency, performance, compliance.
        reason: feature
        # Who asked for the change.
        actor:
        # What the change must do, in sentences a reviewer can check: behaviour, limits, features that must remain.
        acceptance: []
        delta:
          add: []
          remove: []
        tests:
          modify: []
          delete: []

        """;

    public const string Config = """
        spec: 1.0
        languages: [{languages}]
        implementers: [agent-bot]
        paths:
          tests: ["**/tests/**", "**/*.Tests/**", "**/*Tests.cs", "**/*.test.ts", "**/*.spec.ts", "**/test_*.py", "**/*_test.py"]
          generated: ["**/generated/**", "**/*.g.cs", "**/*.generated.*"]
          # Folders and files the gate leaves alone, e.g. [legacy, "src/Migrations", "**/vendor"].
          # The file .c-leanignore, one entry per line, adds to this list.
          excluded: []
        packages:
        {packages}
        {rules}
        tests:
          runtime_budget_seconds: 5
          # Per language: the project's own test command. It must write JUnit XML to $GATE_JUNIT and
          # should run only the files in $GATE_TEST_FILES when that is not empty. `c-lean check` runs it
          # once for the suite, once per mutant of the changed lines, and once instrumented for traces.
          commands: {}
          # Per language: run once in a fresh checkout before the tests (restore, install).
          setup: {}
        judge:
          mode: advisory
          votes: 5
        protected:
          - gate.yaml
          - .c-leanignore
          - architecture.yaml
          - gate.lock
          - .gate/**
          - changes/**
          - corpus/**
          - .github/**
          - .editorconfig
          - "**/.eslintrc*"
          - "**/eslint.config.*"
          - "**/Directory.Build.props"

        """;

    public const string Workflow = """
        name: gate
        on: [pull_request]
        jobs:
          gate:
            runs-on: ubuntu-latest
            steps:
              - uses: actions/checkout@v4
                with:
                  fetch-depth: 0
              # Installs the gate and puts it on the PATH. Pin a tag instead of main once there is one.
              - uses: mrrasmussendk/C-lean@main
              # Hooks give fast feedback only. This job is the gate: exit codes 1, 2 and 3 all fail it.
              - run: c-lean doctor
              - run: c-lean check --base "origin/${{ github.base_ref }}"

        """;

    public const string Owners = """
        # Protected paths of the gate. Name a human owner for each so the implementer cannot merge an edit.
        # /gate.yaml          <owner>
        # /.c-leanignore      <owner>
        # /architecture.yaml  <owner>
        # /gate.lock          <owner>
        # /.gate/             <owner>
        # /changes/           <owner>
        # /.github/           <owner>

        """;

    public const string Hooks = """
        {
          "hooks": {
            "PostToolUse": [
              {
                "matcher": "Edit|Write|MultiEdit",
                "hooks": [{ "type": "command", "command": "c-lean hook" }]
              }
            ]
          }
        }

        """;

    /// <summary>
    /// The OpenCode plugin. After an edit it passes `c-lean hook` the event that hook reads, and adds the findings to
    /// what the agent sees of its own edit.
    /// </summary>
    public const string OpenCodePlugin = """
        // Written by `c-lean init`. After each edit by the agent it checks the edited file with `c-lean hook`
        // and adds the findings to the result of the edit. It gives fast feedback only; CI is the gate.
        import { spawnSync } from "node:child_process"
        import { resolve } from "node:path"

        const PATCHED = /^\*\*\* (?:Add|Update) File: (.+)$/gm

        const edited = (tool, args) => {
          if (tool === "edit" || tool === "write" || tool === "multiedit") return [args?.filePath]
          if (tool === "apply_patch" || tool === "patch") return [...String(args?.patchText ?? "").matchAll(PATCHED)].map((match) => match[1].trim())
          return []
        }

        const findings = (directory, file) => {
          const event = JSON.stringify({ cwd: directory, tool_input: { file_path: resolve(directory, file) } })
          const hook = spawnSync("c-lean", ["hook"], { input: event, encoding: "utf8" })
          return hook.status === 2 ? hook.stderr.trim() : ""
        }

        export const CLean = async ({ directory }) => ({
          "tool.execute.after": async (input, output) => {
            const files = edited(input.tool, input.args).filter((file) => typeof file === "string" && file.length > 0)
            const found = files.map((file) => findings(directory, file)).filter((text) => text.length > 0)
            if (found.length > 0) output.output = [output.output, ...found].join("\n\n")
          },
        })

        """;

    public const string Ignore = """
        # Folders and files the gate leaves alone: one per line, like a .gitignore. A protected file.
        # An entry covers itself and everything under it. Examples:
        #   legacy/
        #   src/Migrations
        #   **/vendor
        #   **/*.designer.cs

        """;

    public const string MutationIgnore = """
        # Mutants the gate does not require a test to kill. A protected file.
        # Each entry may give `path` (glob), `mutator` and `context` (logging or guard).
        ignore:
          - {context: logging}
          - {context: guard}

        """;

    public const string Ignored = """
        out/
        base/
        corpus-out/
        edit/
        log/

        """;

    public const string ProvisionalArchitecture = """
        components:
          - name: all
            paths: ["**"]
        edges: []

        """;
}
