// Espelho TypeScript do contrato definido em docs/protocol-v1.md.
// Cada linha NDJSON é um envelope com v/id/correlationId/timestamp/type/payload.

export const PROTOCOL_VERSION = 1

export const COMMAND_TYPES = {
  promptSubmit: "prompt_submit",
  interruptTurn: "interrupt_turn",
  toolDecision: "tool_decision",
  sessionSwitch: "session_switch",
  sessionClear: "session_clear"
} as const

export const EVENT_TYPES = {
  sessionSnapshot: "session_snapshot",
  streamDelta: "stream_delta",
  toolRequest: "tool_request",
  toolResult: "tool_result",
  contextMetrics: "context_metrics",
  errorEvent: "error_event"
} as const

export const ERROR_CODES = {
  invalidMessage: "invalid_message",
  unsupportedVersion: "unsupported_version",
  emptyPrompt: "empty_prompt",
  unknownCommand: "unknown_command",
  toolDenied: "tool_denied",
  toolTimeout: "tool_timeout",
  llmFailure: "llm_failure",
  sessionFailure: "session_failure",
  internal: "internal"
} as const

export type CommandType = (typeof COMMAND_TYPES)[keyof typeof COMMAND_TYPES]
export type EventType = (typeof EVENT_TYPES)[keyof typeof EVENT_TYPES]

export interface Envelope<Type extends string = string, Payload = unknown> {
  v: number
  id: string
  correlationId: string
  timestamp: string
  type: Type
  payload: Payload
}

export interface PromptSubmitPayload {
  text: string
  sessionId?: string | null
}

export interface InterruptTurnPayload {
  reason?: string | null
}

export interface ToolDecisionPayload {
  toolRequestId: string
  approved: boolean
  reason?: string | null
}

export interface SessionSwitchPayload {
  sessionId: string
}

export interface SessionClearPayload {
  clearHistory?: boolean
}

export interface ContextUsagePayload {
  used: number
  limit: number
  percent: number
}

export interface SessionMessagePayload {
  role: string
  text: string
  timestamp: string
}

export interface SessionSnapshotPayload {
  id: string
  model: string
  agent: string
  cwd: string
  branch: string
  messages: SessionMessagePayload[]
  context: ContextUsagePayload
  updatedAt: string
}

export interface SessionSnapshotEventPayload {
  session: SessionSnapshotPayload
}

export interface StreamDeltaPayload {
  text: string
  final?: boolean
}

export interface ToolRequestPayload {
  toolRequestId: string
  name: string
  input: string
  risk: string
  requiresDecision: boolean
}

export interface ToolResultPayload {
  toolRequestId: string
  name: string
  ok: boolean
  text: string
}

export interface ErrorPayload {
  code: string
  message: string
}

export type CommandEnvelope =
  | Envelope<"prompt_submit", PromptSubmitPayload>
  | Envelope<"interrupt_turn", InterruptTurnPayload>
  | Envelope<"tool_decision", ToolDecisionPayload>
  | Envelope<"session_switch", SessionSwitchPayload>
  | Envelope<"session_clear", SessionClearPayload>

export type EventEnvelope =
  | Envelope<"session_snapshot", SessionSnapshotEventPayload>
  | Envelope<"stream_delta", StreamDeltaPayload>
  | Envelope<"tool_request", ToolRequestPayload>
  | Envelope<"tool_result", ToolResultPayload>
  | Envelope<"context_metrics", ContextUsagePayload>
  | Envelope<"error_event", ErrorPayload>

export type ProtocolMessage = CommandEnvelope | EventEnvelope

export function createEnvelope<Type extends string, Payload>(
  type: Type,
  payload: Payload,
  correlationId = "",
  id: string = crypto.randomUUID()
): Envelope<Type, Payload> {
  return {
    v: PROTOCOL_VERSION,
    id,
    correlationId,
    timestamp: new Date().toISOString(),
    type,
    payload
  }
}

export function encodeCommand<Type extends CommandType, Payload>(
  type: Type,
  payload: Payload,
  correlationId = ""
): string {
  return JSON.stringify(createEnvelope(type, payload, correlationId))
}

export function createErrorEvent(
  code: string,
  message: string,
  correlationId = ""
): EventEnvelope {
  return createEnvelope(EVENT_TYPES.errorEvent, { code, message }, correlationId)
}

export function parseEnvelope(line: string): ProtocolMessage {
  let value: unknown

  try {
    value = JSON.parse(line)
  } catch {
    throw new Error("Linha NDJSON não é um JSON válido.")
  }

  if (typeof value !== "object" || value === null || Array.isArray(value)) {
    throw new Error("Mensagem NDJSON deve ser um objeto.")
  }

  const envelope = value as Record<string, unknown>

  if (envelope.v !== PROTOCOL_VERSION) {
    throw new Error(`Versão do protocolo não suportada: ${String(envelope.v)}.`)
  }

  if (
    typeof envelope.id !== "string" ||
    typeof envelope.correlationId !== "string" ||
    typeof envelope.timestamp !== "string" ||
    typeof envelope.type !== "string"
  ) {
    throw new Error("Envelope incompleto: id/correlationId/timestamp/type são obrigatórios.")
  }

  if (typeof envelope.payload !== "object" || envelope.payload === null) {
    throw new Error("Envelope sem payload.")
  }

  const known = new Set<string>([...Object.values(COMMAND_TYPES), ...Object.values(EVENT_TYPES)])
  if (!known.has(envelope.type)) {
    throw new Error(`Tipo desconhecido: ${envelope.type}.`)
  }

  return envelope as unknown as ProtocolMessage
}
