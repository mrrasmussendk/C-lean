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
        packages:
        {packages}
        rules: {}
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
