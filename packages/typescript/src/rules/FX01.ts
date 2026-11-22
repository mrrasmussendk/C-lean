// FX01: Effect APIs (clock, file system, network, console, randomness, environment, mutable global state) may be called only inside components marked as effect components in the architecture model
import { onNode, productionFiles } from "../analysis/report";
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";

export function FX01(project: Project): Finding[] {
  return productionFiles(project)
    .filter((unit) => !unit.effectComponent)
    .flatMap((unit) =>
      unit.effects.map((use) =>
        onNode("FX01", unit.path, use.node, `${use.api} is a ${use.group} effect used in component ${unit.component || "(unmapped)"}, which is not an effect component. Move the effect into an effect component and pass its result in`),
      ),
    );
}
