// O campo de chat é a única fonte de digitação enquanto o menu "/" está aberto.
// Estas funções puras mantêm o valor do campo e o estado do menu sincronizados.
import {
  reduceCommandMenu,
  type CommandMenuState
} from "./commandMenuState"

export interface CommandInputResult {
  readonly state: CommandMenuState
  readonly value: string
}

function samePath(a: readonly string[], b: readonly string[]): boolean {
  return a.length === b.length && a.every((segment, index) => segment === b[index])
}

export function commandInputChanged(state: CommandMenuState, value: string): CommandInputResult {
  if (value.startsWith("/")) {
    return { state: reduceCommandMenu(state, { kind: "type", value: value.slice(1) }), value }
  }

  if (state.open) {
    return { state: reduceCommandMenu(state, { kind: "close" }), value }
  }

  return { state, value }
}

export function commandInputEnter(state: CommandMenuState, value: string): CommandInputResult {
  if (!state.open) return { state, value }

  const next = reduceCommandMenu(state, { kind: "enter" })
  if (!next.open || next.apiKey) return { state: next, value: "" }

  // Ao descer ou subir de nível, o filtro recomeça; o campo volta a mostrar "/".
  return { state: next, value: samePath(state.path, next.path) ? value : "/" }
}

export function commandInputEscape(state: CommandMenuState, value: string): CommandInputResult {
  if (!state.open) return { state, value }

  const next = reduceCommandMenu(state, { kind: "escape" })
  if (!next.open || next.apiKey) return { state: next, value: "" }
  return { state: next, value: "/" }
}
