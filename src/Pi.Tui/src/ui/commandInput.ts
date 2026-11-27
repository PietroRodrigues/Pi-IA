// O campo de chat é a única fonte de digitação enquanto o menu "/" está aberto.
// Estas funções puras mantêm o valor do campo e o estado do menu sincronizados.
import { COMMANDS, LAYOUT, TEXT } from "../constants"
import {
  reduceCommandMenu,
  type CommandMenuState
} from "./commandMenuState"

export interface CommandInputResult {
  readonly state: CommandMenuState
  readonly value: string
}

export type MenuNavigationAction =
  | typeof COMMANDS.actionKinds.up
  | typeof COMMANDS.actionKinds.down
  | typeof COMMANDS.actionKinds.enter
  | typeof COMMANDS.actionKinds.escape

// Setas do menu: ↑↓ navegam, → confirma como Enter e ← volta/fecha como Esc.
export function menuKeyAction(name: string): MenuNavigationAction | undefined {
  switch (name) {
    case COMMANDS.keys.up:
      return COMMANDS.actionKinds.up
    case COMMANDS.keys.down:
      return COMMANDS.actionKinds.down
    case COMMANDS.keys.right:
      return COMMANDS.actionKinds.enter
    case COMMANDS.keys.left:
      return COMMANDS.actionKinds.escape
    default:
      return undefined
  }
}

function samePath(a: readonly string[], b: readonly string[]): boolean {
  return a.length === b.length && a.every((segment, index) => segment === b[index])
}

export function commandInputChanged(state: CommandMenuState, value: string): CommandInputResult {
  if (value.startsWith(COMMANDS.slash.prefix)) {
    return { state: reduceCommandMenu(state, { kind: COMMANDS.actionKinds.type, value: value.slice(LAYOUT.one) }), value }
  }

  if (state.open) {
    return { state: reduceCommandMenu(state, { kind: COMMANDS.actionKinds.close }), value }
  }

  return { state, value }
}

export function commandInputEnter(state: CommandMenuState, value: string): CommandInputResult {
  if (!state.open) return { state, value }

  const next = reduceCommandMenu(state, { kind: COMMANDS.actionKinds.enter })
  if (!next.open || next.apiKey) return { state: next, value: TEXT.common.empty }

  // Ao descer ou subir de nível, o filtro recomeça; o campo volta a mostrar "/".
  return { state: next, value: samePath(state.path, next.path) ? value : COMMANDS.slash.prefix }
}

export function commandInputEscape(state: CommandMenuState, value: string): CommandInputResult {
  if (!state.open) return { state, value }

  const next = reduceCommandMenu(state, { kind: COMMANDS.actionKinds.escape })
  if (!next.open || next.apiKey) return { state: next, value: TEXT.common.empty }
  return { state: next, value: COMMANDS.slash.prefix }
}
