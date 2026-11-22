// Contrato NDJSON entre TUI e backend: os nomes batem com src/Bridge/BridgeProtocol.cs.
// BridgeInput vai da TUI para o C#; BridgeOutput volta do C# para a TUI.

export interface PromptInput {
  type: "prompt"
  requestId: string
  text: string
}

export interface CancelInput {
  type: "cancel"
  requestId?: string
}

export interface ClearInput {
  type: "clear"
}

export type BridgeInput = PromptInput | CancelInput | ClearInput

export interface ReadyEvent {
  type: "ready"
  model: string
  agent: string
  session: string
  cwd: string
  branch: string
}

export interface TokenEvent {
  type: "token"
  requestId: string
  text: string
}

export interface ThoughtEvent {
  type: "thought"
  requestId: string
  text: string
}

export interface ToolCallEvent {
  type: "tool_call"
  requestId: string
  name: string
  input: string
}

export interface ToolResultEvent {
  type: "tool_result"
  requestId: string
  name: string
  ok: boolean
  text: string
}

export interface ContextStatsEvent {
  type: "context_stats"
  used: number
  limit: number
  percent: number
}

export interface DoneEvent {
  type: "done"
  requestId: string
  text: string
  cancelled: boolean
}

export interface ErrorEvent {
  type: "error"
  requestId: string
  message: string
}

export type BridgeOutput =
  | ReadyEvent
  | TokenEvent
  | ThoughtEvent
  | ToolCallEvent
  | ToolResultEvent
  | ContextStatsEvent
  | DoneEvent
  | ErrorEvent
