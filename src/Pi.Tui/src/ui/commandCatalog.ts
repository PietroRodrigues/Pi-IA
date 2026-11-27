// Árvore de comandos "/" em linguagem simples. Nenhum comando é enviado ao host:
// a navegação aqui é apenas a topologia visual da TUI.
import { COMMANDS, GLYPHS, LAYOUT, LIMITS, TEXT } from "../constants"
import {
  MODEL_PROVIDERS,
  type CatalogSource,
  type InfoSource
} from "./providerCatalog"

export type CommandNodeKind =
  | typeof COMMANDS.nodeKinds.group
  | typeof COMMANDS.nodeKinds.selection
  | typeof COMMANDS.nodeKinds.connect
  | typeof COMMANDS.nodeKinds.localAdd
  | typeof COMMANDS.nodeKinds.info

export interface CommandNode {
  readonly segment: string
  readonly command: string
  readonly description: string
  readonly kind: CommandNodeKind
  readonly source?: CatalogSource | InfoSource
  readonly children?: readonly CommandNode[]
  readonly dynamic?: typeof COMMANDS.catalogKinds.modelProviders
}

export const ROOT_COMMANDS: readonly CommandNode[] = COMMANDS.tree

export function childrenOf(node: CommandNode): readonly CommandNode[] {
  if (node.dynamic === COMMANDS.catalogKinds.modelProviders) {
    return MODEL_PROVIDERS.map(provider => ({
      segment: provider.id,
      command: `${COMMANDS.slash.models}${COMMANDS.slash.prefix}${provider.id}`,
      description: `${TEXT.menu.dynamicDescriptionPrefix}${provider.label}`,
      kind: COMMANDS.nodeKinds.selection,
      source: COMMANDS.catalogKinds.models
    }))
  }

  return node.children ?? []
}

export function resolvePath(path: readonly string[]): CommandNode | undefined {
  return resolveNode(path)
}

// Resolve o nó final de um caminho, incluindo os filhos dinâmicos de /models/providers.
export function resolveNode(path: readonly string[]): CommandNode | undefined {
  if (path.length === LIMITS.zero) return undefined

  let node = ROOT_COMMANDS.find(entry => entry.segment === path[LIMITS.zero])
  if (!node) return undefined

  for (const segment of path.slice(LIMITS.one)) {
    node = childrenOf(node).find(entry => entry.segment === segment)
    if (!node) return undefined
  }

  return node
}

export function parentPath(path: readonly string[]): readonly string[] {
  return path.slice(LIMITS.zero, LAYOUT.cursorStart)
}

export function pathLabel(path: readonly string[]): string {
  return path.length === LIMITS.zero
    ? COMMANDS.slash.root
    : `${COMMANDS.slash.prefix}${path.join(COMMANDS.slash.prefix)}`
}

export function filterCommands(query: string): CommandNode[] {
  const term = query.trim().toLowerCase()
  if (term.length === LIMITS.zero) {
    return [...ROOT_COMMANDS]
  }

  return ROOT_COMMANDS.filter(command =>
    `${command.command}${GLYPHS.space}${command.description}`.toLowerCase().includes(term))
}

// Navegação circular: a seta sempre aponta para um item quando há opções.
export function moveSelection(index: number, count: number, delta: -1 | 1): number {
  if (count <= LIMITS.zero) {
    return LIMITS.zero
  }

  return ((index + delta) % count + count) % count
}
