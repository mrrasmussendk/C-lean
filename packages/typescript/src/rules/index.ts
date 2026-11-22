/** The explicit list of rules this package decides. HD01 runs after them, on their findings. */
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";
import { AR01 } from "./AR01";
import { AR02 } from "./AR02";
import { CP01 } from "./CP01";
import { DC01 } from "./DC01";
import { DC02 } from "./DC02";
import { DC03 } from "./DC03";
import { DS01 } from "./DS01";
import { DS02 } from "./DS02";
import { ER01 } from "./ER01";
import { ER02 } from "./ER02";
import { ER03 } from "./ER03";
import { FR02 } from "./FR02";
import { FX01 } from "./FX01";
import { FX02 } from "./FX02";
import { FX03 } from "./FX03";
import { NM01 } from "./NM01";
import { SZ01 } from "./SZ01";
import { SZ02 } from "./SZ02";
import { SZ03 } from "./SZ03";
import { SZ04 } from "./SZ04";
import { SZ05 } from "./SZ05";
import { SZ06 } from "./SZ06";
import { TS01 } from "./TS01";
import { TS02 } from "./TS02";

export type Rule = (project: Project) => Finding[];

export const RULES: readonly Rule[] = [
  SZ01, SZ02, SZ03, SZ04, SZ05, SZ06,
  AR01, AR02,
  FX01, FX02, FX03,
  ER01, ER02, ER03,
  CP01,
  DC01, DC02, DC03,
  FR02,
  NM01,
  DS01, DS02,
  TS01, TS02,
];
