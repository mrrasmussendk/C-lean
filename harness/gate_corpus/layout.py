"""Where things are in the repository. Pure path arithmetic."""

from dataclasses import dataclass
from pathlib import Path

from gate_corpus.model import Case


@dataclass(frozen=True)
class Layout:
    repository: Path
    language: str

    @property
    def cases(self) -> Path:
        return self.repository / "corpus" / "cases"

    @property
    def defaults(self) -> Path:
        return self.repository / "corpus" / "defaults"

    @property
    def package(self) -> Path:
        return self.repository / "packages" / self.language

    @property
    def command(self) -> Path:
        return self.package / "bin" / f"gate-{self.language}"

    @property
    def rules(self) -> Path:
        return self.repository / "spec" / "rules.yaml"

    def fixture(self, case: Case) -> Path:
        return self.repository / "corpus" / "fixtures" / self.language / case.suite / case.case_id

    def out(self, case: Case) -> Path:
        return self.repository / ".gate" / "corpus-out" / self.language / case.suite / case.case_id
