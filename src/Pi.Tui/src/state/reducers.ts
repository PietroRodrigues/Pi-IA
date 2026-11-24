// Redutor puro do estado da sessão. Não conhece Solid, ponte nem terminal:
// recebe ações (inclusive eventos do protocolo) e devolve um novo estado.
import {
  EVENT_TYPES,
  type ContextUsagePayload,
  type ErrorPayload,
  type EventEnvelope,
  type SessionSnapshotPayload,
  type ToolRequestPayload,
  type ToolResultPayload
} from "../protocol/schema"

export type MessageRole = "user" | "assistant" | "system" | "info" | "thought" | "tool"

export interface ChatMessage {
  id: string
  role: MessageRole
  text: string
  streaming: boolean
  name?: string
  ok?: boolean
}

export interface SessionInfo {
  model: string
  agent: string
  session: string
  cwd: string
  branch: string
}

export interface SessionState {
  messages: ChatMessage[]
  context: ContextUsagePayload
  session: SessionInfo
  status: string
  jump: boolean
  activeCorrelationId?: string
  awaitingDone: string[]
  cancelled: string[]
  pendingToolRequest?: ToolRequestPayload
  awaitingSnapshot: boolean
}

export type SessionAction =
  | { kind: "event"; event: EventEnvelope }
  | { kind: "submit"; correlationId: string; text: string }
  | { kind: "cancel"; correlationId?: string }
  | { kind: "decision"; toolRequestId: string }
  | { kind: "clear" }
  | { kind: "status"; status: string }
  | { kind: "jump"; jump: boolean }
  | { kind: "system"; message: string; role?: "system" | "info" }

export const initialSessionState: SessionState = {
  messages: [],
  context: { used: 0, limit: 32768, percent: 0 },
  session: { model: "…", agent: "…", session: "Sessão Pi", cwd: "…", branch: "…" },
  status: "conectando…",
  jump: false,
  awaitingDone: [],
  cancelled: [],
  awaitingSnapshot: true
}

const newId = (prefix: string) => `${prefix}-${crypto.randomUUID()}`

function assistantId(correlationId: string): string {
  return `assistant-${correlationId}`
}

function ensureAssistant(messages: ChatMessage[], correlationId: string): ChatMessage[] {
  const id = assistantId(correlationId)
  return messages.some(message => message.id === id)
    ? messages
    : [...messages, { id, role: "assistant", text: "", streaming: true }]
}

function appendActivity(
  state: SessionState,
  correlationId: string,
  role: "thought" | "tool",
  text: string,
  name?: string,
  ok?: boolean
): SessionState {
  if (!state.awaitingDone.includes(correlationId)) return state

  return {
    ...state,
    messages: [
      ...state.messages,
      { id: newId(role), role, text, streaming: false, name, ok }
    ]
  }
}

function finish(
  state: SessionState,
  correlationId: string,
  fallbackText = ""
): SessionState {
  const id = assistantId(correlationId)
  let messages = ensureAssistant(state.messages, correlationId)
  const cancelled = state.cancelled.includes(correlationId)

  messages = messages.map(message => {
    if (message.id !== id) return message

    let text = message.text
    if (text.length === 0 && fallbackText.length > 0) text = fallbackText
    if (cancelled) text += text.length > 0 ? "\n\n[cancelado]" : "[cancelado]"

    return { ...message, text, streaming: false }
  })

  const awaitingDone = state.awaitingDone.filter(item => item !== correlationId)
  const activeCorrelationId =
    state.activeCorrelationId === correlationId ? undefined : state.activeCorrelationId

  return {
    ...state,
    messages,
    awaitingDone,
    activeCorrelationId,
    cancelled: state.cancelled.filter(item => item !== correlationId),
    pendingToolRequest: undefined,
    status: cancelled ? "cancelado" : state.status,
    jump: activeCorrelationId === undefined ? state.jump : state.jump
  }
}

function fromSnapshot(snapshot: SessionSnapshotPayload): ChatMessage[] {
  return snapshot.messages.map((message, index) => ({
    id: `snapshot-${index}-${message.role}-${snapshot.id}`,
    role: message.role === "user" || message.role === "assistant"
      ? message.role
      : message.role === "system" ? "system" : "info",
    text: message.text,
    streaming: false
  }))
}

function reduceEvent(state: SessionState, event: EventEnvelope): SessionState {
  const correlationId = event.correlationId

  switch (event.type) {
    case EVENT_TYPES.sessionSnapshot: {
      const snapshot = event.payload.session
      const messages =
        state.awaitingSnapshot || state.messages.length === 0
          ? fromSnapshot(snapshot)
          : state.messages

      return {
        ...state,
        messages,
        awaitingSnapshot: false,
        session: {
          model: snapshot.model,
          agent: snapshot.agent,
          session: snapshot.id,
          cwd: snapshot.cwd,
          branch: snapshot.branch
        },
        context: snapshot.context,
        status: state.status === "conectando…" ? "pronto" : state.status
      }
    }

    case EVENT_TYPES.contextMetrics:
      return { ...state, context: event.payload }

    case EVENT_TYPES.streamDelta:
      if (event.payload.final) {
        return finish(state, correlationId)
      }

      if (state.activeCorrelationId !== correlationId) return state

      return {
        ...state,
        messages: ensureAssistant(state.messages, correlationId).map(message =>
          message.id === assistantId(correlationId)
            ? { ...message, text: message.text + event.payload.text }
            : message
        )
      }

    case EVENT_TYPES.toolRequest: {
      const request: ToolRequestPayload = event.payload
      const withActivity = appendActivity(
        state,
        correlationId,
        "tool",
        request.input,
        request.name
      )

      return {
        ...withActivity,
        pendingToolRequest: request.requiresDecision ? request : undefined,
        status: request.requiresDecision
          ? `aprovação · ${request.name}`
          : `ferramenta · ${request.name}`
      }
    }

    case EVENT_TYPES.toolResult: {
      const result: ToolResultPayload = event.payload
      const withActivity = appendActivity(
        state,
        correlationId,
        "tool",
        result.text,
        result.name,
        result.ok
      )

      return {
        ...withActivity,
        pendingToolRequest:
          state.pendingToolRequest?.toolRequestId === result.toolRequestId
            ? undefined
            : state.pendingToolRequest,
        status: result.ok
          ? `ferramenta ${result.name} concluída`
          : `ferramenta ${result.name} falhou`
      }
    }

    case EVENT_TYPES.errorEvent: {
      const error: ErrorPayload = event.payload
      const withMessage: SessionState = {
        ...state,
        messages: [
          ...state.messages,
          {
            id: newId("system"),
            role: "system",
            text: `[${error.code}] ${error.message}`,
            streaming: false
          }
        ],
        status: "erro"
      }

      return state.awaitingDone.includes(correlationId)
        ? finish(withMessage, correlationId)
        : withMessage
    }

    default:
      return state
  }
}

export function reduceSession(state: SessionState, action: SessionAction): SessionState {
  switch (action.kind) {
    case "event":
      return reduceEvent(state, action.event)

    case "submit":
      if (state.activeCorrelationId) return state

      return {
        ...state,
        activeCorrelationId: action.correlationId,
        awaitingDone: [...state.awaitingDone, action.correlationId],
        cancelled: state.cancelled.filter(id => id !== action.correlationId),
        pendingToolRequest: undefined,
        messages: [
          ...state.messages,
          { id: newId("user"), role: "user", text: action.text, streaming: false },
          { id: assistantId(action.correlationId), role: "assistant", text: "", streaming: true }
        ],
        status: "pensando…"
      }

    case "cancel": {
      const target = action.correlationId ?? state.activeCorrelationId
      if (!target) return state

      return {
        ...state,
        activeCorrelationId:
          state.activeCorrelationId === target ? undefined : state.activeCorrelationId,
        cancelled: state.cancelled.includes(target)
          ? state.cancelled
          : [...state.cancelled, target],
        pendingToolRequest: undefined,
        status: "cancelando…"
      }
    }

    case "decision":
      return {
        ...state,
        pendingToolRequest:
          state.pendingToolRequest?.toolRequestId === action.toolRequestId
            ? undefined
            : state.pendingToolRequest,
        status: state.pendingToolRequest?.toolRequestId === action.toolRequestId
          ? "aprovação registrada"
          : state.status
      }

    case "clear":
      return {
        ...initialSessionState,
        messages: [],
        awaitingSnapshot: false,
        session: state.session,
        context: { ...state.context, used: 0, percent: 0 },
        status: "contexto limpo"
      }

    case "status":
      return { ...state, status: action.status }

    case "jump":
      return { ...state, jump: action.jump }

    case "system":
      return {
        ...state,
        messages: [
          ...state.messages,
          {
            id: newId(action.role ?? "system"),
            role: action.role ?? "system",
            text: action.message,
            streaming: false
          }
        ]
      }
  }
}
