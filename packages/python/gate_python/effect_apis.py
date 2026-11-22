"""The standard-library and runtime APIs that are effects, by group. The one list FX01 reads."""

from typing import Mapping

CLOCK = "clock"
FILES = "file system"
NETWORK = "network"
CONSOLE = "console"
RANDOMNESS = "randomness"
ENVIRONMENT = "environment"
GLOBAL_STATE = "mutable global state"

PATH_METHODS = (
    "read_text", "read_bytes", "write_text", "write_bytes", "open", "exists", "is_file", "is_dir", "mkdir", "rmdir",
    "unlink", "rename", "replace", "touch", "stat", "iterdir", "glob", "rglob", "resolve",
)

APIS: Mapping[str, tuple[str, ...]] = {
    CLOCK: (
        "time.time", "time.time_ns", "time.monotonic", "time.monotonic_ns", "time.perf_counter",
        "time.perf_counter_ns", "time.process_time", "time.localtime", "time.gmtime", "time.sleep",
        "datetime.datetime.now", "datetime.datetime.utcnow", "datetime.datetime.today", "datetime.date.today",
    ),
    FILES: (
        "builtins.open", "io.open", "os.open", "os.remove", "os.unlink", "os.rename", "os.replace", "os.mkdir",
        "os.makedirs", "os.rmdir", "os.removedirs", "os.listdir", "os.scandir", "os.walk", "os.stat", "os.chmod",
        "os.path.exists", "os.path.isfile", "os.path.isdir", "os.path.getsize", "os.path.getmtime", "glob.glob",
        "glob.iglob", *(f"pathlib.Path.{method}" for method in PATH_METHODS),
    ),
    NETWORK: ("urllib.request.urlopen", "urllib.request.urlretrieve"),
    CONSOLE: ("builtins.print", "builtins.input", "sys.stdout", "sys.stderr", "sys.stdin", "pprint.pprint"),
    RANDOMNESS: ("uuid.uuid1", "uuid.uuid4", "os.urandom"),
    ENVIRONMENT: (
        "os.environ", "os.getenv", "os.putenv", "os.unsetenv", "os.getcwd", "os.chdir", "os.path.expanduser",
        "os.path.expandvars", "pathlib.Path.cwd", "pathlib.Path.home",
    ),
}

MODULES: Mapping[str, tuple[str, ...]] = {
    FILES: ("shutil", "tempfile"),
    NETWORK: ("socket", "ssl", "http.client", "requests", "httpx", "smtplib", "ftplib"),
    RANDOMNESS: ("random", "secrets"),
}

GROUP_OF_API: Mapping[str, str] = {api: group for group, apis in APIS.items() for api in apis}
GROUP_OF_MODULE: Mapping[str, str] = {module: group for group, modules in MODULES.items() for module in modules}


def group_of(name: str) -> str:
    """The effect group of a qualified name, or nothing when it is not an effect API."""
    if name in GROUP_OF_API:
        return GROUP_OF_API[name]
    owners = [group for module, group in GROUP_OF_MODULE.items() if name.startswith(f"{module}.")]
    return owners[0] if owners else ""
