// Redutor puro do estado da sessão. Não conhece Solid, ponte nem terminal:
// recebe ações (inclusive eventos do protocolo) e devolve um novo estado.
import { COMMANDS, GLYPHS, LIMITS, PROTOCOL, ROLES, STATUSES, TEXT } from "../constants"
import {
  type ContextUsagePayload,
  type ErrorPayload,
  type EventEnvelope,
  type SessionSnapshotPayload,
  type ToolRequestPayload,
  type ToolResultPayload
} from "../protocol/schema"

export type MessageRole = (typeof ROLES)[keyof typeof ROLES]

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
  | { kind: typeof COMMANDS.actionKinds.event; event: EventEnvelope }
  | {
      kind: typeof COMMANDS.actionKinds.submit
      correlationId: string
      text: string
    }
  | { kind: typeof COMMANDS.actionKinds.cancel; correlationId?: string }
  | { kind: typeof COMMANDS.actionKinds.decision; toolRequestId: string }
  | { kind: typeof COMMANDS.actionKinds.clear }
  | { kind: typeof COMMANDS.actionKinds.status; status: string }
  | { kind: typeof COMMANDS.actionKinds.jump; jump: boolean }
  | {
      kind: typeof COMMANDS.actionKinds.system
      message: string
      role?: typeof ROLES.system | typeof ROLES.info
    }

export const initialSessionState: SessionState = {
  messages: [],
  context: { used: LIMITS.zero, limit: LIMITS.contextTokens, percent: LIMITS.zero },
  session: {
    model: TEXT.common.ellipsis,
    agent: TEXT.common.ellipsis,
    session: TEXT.session.name,
    cwd: TEXT.common.ellipsis,
    branch: TEXT.common.ellipsis
  },
  status: STATUSES.connecting,
  jump: false,
  awaitingDone: [],
  cancelled: [],
  awaitingSnapshot: true
}

const newId = (prefix: string) => [prefix, crypto.randomUUID()].join(TEXT.ids.separator)

function assistantId(correlationId: string): string {
  return [ROLES.assistant, correlationId].join(TEXT.ids.separator)
}

function ensureAssistant(messages: ChatMessage[], correlationId: string): ChatMessage[] {
  const id = assistantId(correlationId)
  return messages.some(message => message.id === id)
    ? messages
    : [...messages, { id, role: ROLES.assistant, text: TEXT.common.empty, streaming: true }]
}

function appendActivity(
  state: SessionState,
  correlationId: string,
  role: typeof ROLES.thought | typeof ROLES.tool,
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
  fallbackText = TEXT.common.empty
): SessionState {
  const id = assistantId(correlationId)
  let messages = ensureAssistant(state.messages, correlationId)
  const cancelled = state.cancelled.includes(correlationId)

  messages = messages.map(message => {
    if (message.id !== id) return message

    let text = message.text
    if (text.length === 0 && fallbackText.length > 0) text = fallbackText
    if (cancelled) {
      text +=
        text.length > 0
          ? `${TEXT.common.newline}${TEXT.common.newline}${STATUSES.cancelledMarker}`
          : STATUSES.cancelledMarker
    }

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
    status: cancelled ? STATUSES.cancelled : state.status,
    jump: activeCorrelationId === undefined ? state.jump : state.jump
  }
}

function fromSnapshot(snapshot: SessionSnapshotPayload): ChatMessage[] {
  return snapshot.messages.map((message, index) => ({
    id: [
      TEXT.ids.prefixes.snapshot,
      String(index),
      message.role,
      snapshot.id
    ].join(TEXT.ids.separator),
    role:
      message.role === ROLES.user || message.role === ROLES.assistant
        ? message.role
        : message.role === ROLES.system ? ROLES.system : ROLES.info,
    text: message.text,
    streaming: false
  }))
}

function reduceEvent(state: SessionState, event: EventEnvelope): SessionState {
  const correlationId = event.correlationId

  switch (event.type) {
    case PROTOCOL.eventTypes.sessionSnapshot: {
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
        status: state.status === STATUSES.connecting ? STATUSES.ready : state.status
      }
    }

    case PROTOCOL.eventTypes.contextMetrics:
      return { ...state, context: event.payload }

    case PROTOCOL.eventTypes.streamDelta:
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

    case PROTOCOL.eventTypes.toolRequest: {
      const request: ToolRequestPayload = event.payload
      const withActivity = appendActivity(
        state,
        correlationId,
        ROLES.tool,
        request.input,
        request.name
      )

      return {
        ...withActivity,
        pendingToolRequest: request.requiresDecision ? request : undefined,
        status: request.requiresDecision
          ? `${STATUSES.approval}${TEXT.common.separator}${request.name}`
          : `${STATUSES.tool}${TEXT.common.separator}${request.name}`
      }
    }

    case PROTOCOL.eventTypes.toolResult: {
      const result: ToolResultPayload = event.payload
      const withActivity = appendActivity(
        state,
        correlationId,
        ROLES.tool,
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
          ? [STATUSES.tool, result.name, STATUSES.toolDone].join(GLYPHS.space)
          : [STATUSES.tool, result.name, STATUSES.toolFailed].join(GLYPHS.space)
      }
    }

    case PROTOCOL.eventTypes.errorEvent: {
      const error: ErrorPayload = event.payload
      const withMessage: SessionState = {
        ...state,
        messages: [
          ...state.messages,
          {
            id: newId(ROLES.system),
            role: ROLES.system,
            text: `${TEXT.session.errorTextPrefix}${error.code}${TEXT.session.errorTextSeparator}${error.message}`,
            streaming: false
          }
        ],
        status: STATUSES.error
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
    case COMMANDS.actionKinds.event:
      return reduceEvent(state, action.event)

    case COMMANDS.actionKinds.submit:
      if (state.activeCorrelationId) return state

      return {
        ...state,
        activeCorrelationId: action.correlationId,
        awaitingDone: [...state.awaitingDone, action.correlationId],
        cancelled: state.cancelled.filter(id => id !== action.correlationId),
        pendingToolRequest: undefined,
        messages: [
          ...state.messages,
          {
            id: newId(ROLES.user),
            role: ROLES.user,
            text: action.text,
            streaming: false
          },
          {
            id: assistantId(action.correlationId),
            role: ROLES.assistant,
            text: TEXT.common.empty,
            streaming: true
          }
        ],
        status: STATUSES.thinking
      }

    case COMMANDS.actionKinds.cancel: {
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
        status: STATUSES.cancelling
      }
    }

    case COMMANDS.actionKinds.decision:
      return {
        ...state,
        pendingToolRequest:
          state.pendingToolRequest?.toolRequestId === action.toolRequestId
            ? undefined
            : state.pendingToolRequest,
        status:
          state.pendingToolRequest?.toolRequestId === action.toolRequestId
            ? STATUSES.approvalRegistered
            : state.status
      }

    case COMMANDS.actionKinds.clear:
      return {
        ...initialSessionState,
        messages: [],
        awaitingSnapshot: false,
        session: state.session,
        context: { ...state.context, used: LIMITS.zero, percent: LIMITS.zero },
        status: STATUSES.contextCleared
      }

    case COMMANDS.actionKinds.status:
      return { ...state, status: action.status }

    case COMMANDS.actionKinds.jump:
      return { ...state, jump: action.jump }

    case COMMANDS.actionKinds.system:
      return {
        ...state,
        messages: [
          ...state.messages,
          {
            id: newId(action.role ?? ROLES.system),
            role: action.role ?? ROLES.system,
            text: action.message,
            streaming: false
          }
        ]
      }
  }
}
