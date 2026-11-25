// Decisão do Ctrl+C: copiar seleção, cancelar turno ou não fazer nada.
// Esta TUI nunca encerra por Ctrl+C; a saída é explicitamente um comando.
export type CtrlCAction = "copy" | "cancel" | "none"

export interface CtrlCContext {
  readonly hasScreenSelection: boolean
  readonly hasInputSelection: boolean
  readonly busy: boolean
}

export function ctrlCAction(context: CtrlCContext): CtrlCAction {
  if (context.hasScreenSelection || context.hasInputSelection) return "copy"
  if (context.busy) return "cancel"
  return "none"
}

export const EXIT_COMMANDS = ["/sair", "/exit"] as const

export function isExitCommand(value: string): boolean {
  const normalized = value.trim().toLowerCase()
  return (EXIT_COMMANDS as readonly string[]).includes(normalized)
}
