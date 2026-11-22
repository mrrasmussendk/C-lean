class Builder:
    def __init__(self) -> None:
        self.parts: list[str] = []

    def with_a(self) -> "Builder":
        self.parts.append("a")
        return self

    def with_b(self) -> "Builder":
        self.parts.append("b")
        return self

    def with_c(self) -> "Builder":
        self.parts.append("c")
        return self

    def build(self) -> str:
        return "".join(self.parts)


def make(builder: Builder) -> str:
    return builder.with_a().with_b().with_c().build()
