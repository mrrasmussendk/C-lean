#!/bin/sh
# Installs the gate: the core and the native package of every language whose toolchain is here.
#
#   curl -fsSL https://raw.githubusercontent.com/mrrasmussendk/C-lean/main/install.sh | sh
#   or, from a clone:  ./install.sh
#
# Run it again to update. Settings (environment variables):
#   GATE_HOME  where the gate lives        default: this clone, or ~/.gate when piped
#   GATE_BIN   where the commands are put  default: ~/.local/bin
#   GATE_REF   branch or tag to install    default: main
#   GATE_REPO  where to clone from         default: https://github.com/mrrasmussendk/C-lean.git
set -eu

REPO="${GATE_REPO:-https://github.com/mrrasmussendk/C-lean.git}"
REF="${GATE_REF:-main}"
BIN="${GATE_BIN:-$HOME/.local/bin}"

say() { printf 'gate install: %s\n' "$1"; }
fail() { printf 'gate install: %s\n' "$1" >&2; exit 1; }
have() { command -v "$1" >/dev/null 2>&1; }

# A clone that runs its own install.sh is installed in place; anything else goes to GATE_HOME.
here="$(cd "$(dirname "$0")" 2>/dev/null && pwd || true)"
if [ -z "${GATE_HOME:-}" ] && [ -f "$here/core/Gate.sln" ]; then
  HOME_DIR="$here"
else
  HOME_DIR="${GATE_HOME:-$HOME/.gate}"
  have git || fail "git is not installed. Install git and run this again."
  if [ -d "$HOME_DIR/.git" ]; then
    say "updating $HOME_DIR to $REF"
    git -C "$HOME_DIR" fetch --quiet --depth 1 origin "$REF" || fail "cannot fetch $REF from $REPO. Check the name and your access to the repository."
    git -C "$HOME_DIR" checkout --quiet --force FETCH_HEAD
  elif [ -e "$HOME_DIR" ]; then
    fail "$HOME_DIR exists and is not a clone of the gate. Remove it or set GATE_HOME to another directory."
  else
    say "cloning $REPO ($REF) into $HOME_DIR"
    git clone --quiet --depth 1 --branch "$REF" "$REPO" "$HOME_DIR" || fail "cannot clone $REPO at $REF. If the repository is private, clone it yourself and run ./install.sh inside the clone."
  fi
fi

# The core is C#; without the .NET SDK there is no gate.
have dotnet || fail "the .NET SDK is not installed, and the core needs it (version 10 or later). Install it from https://dotnet.microsoft.com/download and run this again."
say "building the core"
dotnet build "$HOME_DIR/core" -c Release --nologo -v q >"$HOME_DIR/.install.log" 2>&1 || fail "the core did not build. The build output is in $HOME_DIR/.install.log"

installed="csharp"
say "building the C# package"
"$HOME_DIR/packages/csharp/bin/gate-csharp" --version >>"$HOME_DIR/.install.log" 2>&1 || fail "the C# package did not build. The build output is in $HOME_DIR/.install.log"

if have node && have npm; then
  say "building the TypeScript package"
  (cd "$HOME_DIR/packages/typescript" && npm ci --silent && npm run --silent build) >>"$HOME_DIR/.install.log" 2>&1 || fail "the TypeScript package did not build. The build output is in $HOME_DIR/.install.log"
  installed="$installed typescript"
else
  say "skipping the TypeScript package: node and npm are not installed (Node 22 or later)"
fi

if have python3; then
  if ! python3 -c "import yaml" 2>/dev/null; then
    say "installing PyYAML for the Python package"
    python3 -m pip install --quiet --user pyyaml >>"$HOME_DIR/.install.log" 2>&1 || python3 -m pip install --quiet --user --break-system-packages pyyaml >>"$HOME_DIR/.install.log" 2>&1 || fail "PyYAML could not be installed, and the Python package needs it. Install it yourself (python3 -m pip install pyyaml) and run this again."
  fi
  installed="$installed python"
else
  say "skipping the Python package: python3 is not installed (Python 3.12 or later)"
fi

# One small command per program, so nothing depends on where the gate lives.
mkdir -p "$BIN"
command_for() {
  printf '#!/bin/sh\nexec "%s" "$@"\n' "$2" > "$BIN/$1"
  chmod +x "$BIN/$1"
}
command_for gate "$HOME_DIR/core/bin/gate"
command_for gate-corpus "$HOME_DIR/harness/bin/gate-corpus"
for language in $installed; do
  command_for "gate-$language" "$HOME_DIR/packages/$language/bin/gate-$language"
done

"$BIN/gate" explain SZ01 >/dev/null 2>&1 || fail "the gate was built and does not run. Try: $BIN/gate explain SZ01"
say "installed: gate, and the packages for $installed, in $BIN"
case ":$PATH:" in
  *":$BIN:"*) ;;
  *) say "$BIN is not on your PATH. Add it:  export PATH=\"$BIN:\$PATH\"" ;;
esac
say "next, in a repository:  gate init"
