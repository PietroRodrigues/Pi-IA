// Store reativo da TUI. Só expõe sinais e encaminha ações ao redutor puro.
import { createSignal, type Accessor } from "solid-js"
import { COMMANDS } from "../constants"
import type { ContextUsagePayload, EventEnvelope, ToolRequestPayload } from "../protocol/schema"
import {
  initialSessionState,
  reduceSession,
  type ChatMessage,
  type SessionAction,
  type SessionInfo,
  type SessionState
} from "./reducers"

export interface SessionStore {
  state: Accessor<SessionState>
  messages: Accessor<ChatMessage[]>
  context: Accessor<ContextUsagePayload>
  session: Accessor<SessionInfo>
  status: Accessor<string>
  jump: Accessor<boolean>
  activeRequest: Accessor<string | undefined>
  busy: Accessor<boolean>
  pendingToolRequest: Accessor<ToolRequestPayload | undefined>
  dispatch: (action: SessionAction) => void
  applyEvent: (event: EventEnvelope) => void
  isActive: (correlationId: string) => boolean
  isAwaitingDone: (correlationId: string) => boolean
}

export function createSessionStore(): SessionStore {
  const [state, setState] = createSignal<SessionState>(initialSessionState)

  const dispatch = (action: SessionAction) =>
    setState(current => reduceSession(current, action))

  return {
    state,
    messages: () => state().messages,
    context: () => state().context,
    session: () => state().session,
    status: () => state().status,
    jump: () => state().jump,
    activeRequest: () => state().activeCorrelationId,
    busy: () => state().activeCorrelationId !== undefined,
    pendingToolRequest: () => state().pendingToolRequest,
    dispatch,
    applyEvent: event => dispatch({ kind: COMMANDS.actionKinds.event, event }),
    isActive: correlationId => state().activeCorrelationId === correlationId,
    isAwaitingDone: correlationId => state().awaitingDone.includes(correlationId)
  }
}
