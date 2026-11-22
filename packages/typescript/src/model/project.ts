/** The analyzed project: what every rule takes as its input. Data only. */
import type * as ts from "typescript";
import type { GateConfig, GlossaryTerm } from "./config";
import type { CompilerFacts } from "./inputs";

type FunctionLike =
  | ts.FunctionDeclaration
  | ts.MethodDeclaration
  | ts.ConstructorDeclaration
  | ts.GetAccessorDeclaration
  | ts.SetAccessorDeclaration
  | ts.FunctionExpression
  | ts.ArrowFunction;

/** A named or anonymous callable with a body. */
export type FunctionNode = FunctionLike & { body: ts.ConciseBody };

export type TypeDeclaration =
  | ts.ClassDeclaration
  | ts.InterfaceDeclaration
  | ts.EnumDeclaration
  | ts.TypeAliasDeclaration;

export interface Comment {
  text: string;
  line: number;
  endLine: number;
  ownLine: boolean;
}

export interface EffectUse {
  node: ts.Node;
  group: string;
  api: string;
}

export interface Suppression {
  line: number;
  rule: string;
  reason: string;
  expires: string;
  approver: string;
}

export interface FunctionUnit {
  node: FunctionNode;
  id: string;
  name: string;
  path: string;
  line: number;
  anonymous: boolean;
  test: boolean;
  sloc: number;
  effects: EffectUse[];
}

export interface TypeUnit {
  node: TypeDeclaration;
  id: string;
  name: string;
  kind: string;
  path: string;
  line: number;
  test: boolean;
  sloc: number;
}

export interface SourceUnit {
  path: string;
  source: ts.SourceFile;
  test: boolean;
  component: string;
  effectComponent: boolean;
  comments: Comment[];
  suppressions: Suppression[];
  functions: FunctionUnit[];
  types: TypeUnit[];
  effects: EffectUse[];
}

export interface Project {
  files: SourceUnit[];
  checker: ts.TypeChecker;
  config: GateConfig;
  today: string;
  glossary: GlossaryTerm[];
  compiler: CompilerFacts;
}
