// Decisão do Ctrl+C: copiar seleção, cancelar turno ou não fazer nada.
// Esta TUI nunca encerra por Ctrl+C; a saída é explicitamente um comando.
import { COMMANDS } from "../constants"

export type CtrlCAction = (typeof COMMANDS.ctrlC)[keyof typeof COMMANDS.ctrlC]

export interface CtrlCContext {
  readonly hasScreenSelection: boolean
  readonly hasInputSelection: boolean
  readonly busy: boolean
}

export function ctrlCAction(context: CtrlCContext): CtrlCAction {
  if (context.hasScreenSelection || context.hasInputSelection) return COMMANDS.ctrlC.copy
  if (context.busy) return COMMANDS.ctrlC.cancel
  return COMMANDS.ctrlC.none
}

export function isExitCommand(value: string): boolean {
  const normalized = value.trim().toLowerCase()
  return (COMMANDS.slash.exit as readonly string[]).includes(normalized)
}
