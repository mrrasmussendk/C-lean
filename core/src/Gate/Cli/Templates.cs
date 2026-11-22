namespace Gate.Cli;

/// <summary>The files `gate init` and `gate change new` write.</summary>
internal static class Templates
{
    public const string ChangeRecord = """
        id: {id}
        # Exactly one of: feature, fix, refactor, dependency, performance, compliance.
        reason: feature
        # Who asked for the change.
        actor:
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
        rules: {}
        tests:
          runtime_budget_seconds: 5
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
              # Hooks give fast feedback only. This job is the gate: exit codes 1, 2 and 3 all fail it.
              - run: gate doctor
              - run: gate check --base "origin/${{ github.base_ref }}"

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
                "matcher": "Edit|Write",
                "hooks": [{ "type": "command", "command": "gate check --stage rules || true" }]
              }
            ]
          }
        }

        """;

    public const string Ignored = """
        out/
        base/
        corpus-out/

        """;

    public const string ProvisionalArchitecture = """
        components:
          - name: all
            paths: ["**"]
        edges: []

        """;
}
