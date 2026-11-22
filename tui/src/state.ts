// Todo o estado reativo do chat: mensagens, status e o pedido em andamento.
// A ponte só alimenta este estado; ela não decide como a tela se comporta.
import { createSignal, type Accessor } from "solid-js"

export type MessageRole = "user" | "assistant" | "system" | "info" | "thought" | "tool"

export interface ChatMessage {
  id: string
  role: MessageRole
  text: string
  streaming: boolean
  name?: string
  ok?: boolean
}

export interface ContextStats {
  used: number
  limit: number
  percent: number
}

export interface SessionInfo {
  model: string
  agent: string
  session: string
  cwd: string
  branch: string
}

export interface ChatState {
  messages: Accessor<ChatMessage[]>
  stats: Accessor<ContextStats>
  session: Accessor<SessionInfo>
  status: Accessor<string>
  jump: Accessor<boolean>
  activeRequest: Accessor<string | undefined>
  busy: Accessor<boolean>
  isActive: (requestId: string) => boolean
  isAwaitingDone: (requestId: string) => boolean
  setStats: (stats: ContextStats) => void
  setSession: (session: SessionInfo) => void
  setStatus: (status: string) => void
  setJump: (jump: boolean) => void
  addUser: (text: string) => void
  startRequest: (requestId: string) => void
  appendToken: (requestId: string, text: string) => void
  addActivity: (
    requestId: string,
    role: "thought" | "tool",
    text: string,
    name?: string,
    ok?: boolean
  ) => void
  finishRequest: (requestId: string, text: string, cancelled: boolean) => void
  failRequest: (requestId: string, message: string) => void
  cancelActive: () => string | undefined
  setSystemError: (message: string) => void
  setSystemInfo: (message: string) => void
  clear: () => void
}

export function createChatState(): ChatState {
  const [messages, setMessages] = createSignal<ChatMessage[]>([])
  const [stats, setStats] = createSignal<ContextStats>({ used: 0, limit: 32768, percent: 0 })
  const [session, setSession] = createSignal<SessionInfo>({
    model: "…",
    agent: "…",
    session: "Sessão Pi",
    cwd: "…",
    branch: "…"
  })
  const [status, setStatus] = createSignal("conectando…")
  const [jump, setJump] = createSignal(false)
  const [activeRequest, setActiveRequest] = createSignal<string | undefined>(undefined)
  const [busy, setBusy] = createSignal(false)

  const requestMessages = new Map<string, string>()
  // Depois de cancelar, novos tokens são ignorados, mas o `done` ainda precisa
  // fechar a bolha — por isso o pedido permanece neste conjunto até lá.
  const awaitingDone = new Set<string>()

  const updateMessage = (id: string, update: (message: ChatMessage) => ChatMessage) => {
    setMessages(current => current.map(message => (message.id === id ? update(message) : message)))
  }

  const ensureAssistant = (requestId: string) => {
    const existingId = requestMessages.get(requestId)
    if (existingId) return existingId

    const id = `assistant-${requestId}`
    requestMessages.set(requestId, id)
    setMessages(current => [
      ...current,
      { id, role: "assistant", text: "", streaming: true }
    ])
    return id
  }

  const closeRequest = (requestId: string) => {
    awaitingDone.delete(requestId)
    if (activeRequest() === requestId) {
      setActiveRequest(undefined)
      setBusy(false)
    }
  }

  const finishAssistant = (requestId: string, text: string, cancelled: boolean) => {
    const id = ensureAssistant(requestId)
    updateMessage(id, message => {
      let content = message.text
      if (content.length === 0 && text.length > 0) content = text
      if (cancelled) content += content.length > 0 ? "\n\n[cancelado]" : "[cancelado]"
      return { ...message, text: content, streaming: false }
    })
  }

  const isActive = (requestId: string) => activeRequest() === requestId

  return {
    messages,
    stats,
    session,
    status,
    jump,
    activeRequest,
    busy,
    isActive,
    isAwaitingDone: requestId => awaitingDone.has(requestId),
    setStats,
    setSession,
    setStatus,
    setJump,
    addUser: text => {
      setMessages(current => [
        ...current,
        {
          id: `user-${crypto.randomUUID()}`,
          role: "user",
          text,
          streaming: false
        }
      ])
    },
    startRequest: requestId => {
      if (awaitingDone.has(requestId)) return

      awaitingDone.add(requestId)
      setActiveRequest(requestId)
      setBusy(true)
      ensureAssistant(requestId)
    },
    appendToken: (requestId, text) => {
      if (!isActive(requestId)) return
      const id = ensureAssistant(requestId)
      updateMessage(id, message => ({ ...message, text: message.text + text }))
    },
    addActivity: (requestId, role, text, name, ok) => {
      if (!isActive(requestId)) return

      setMessages(current => [
        ...current,
        {
          id: `${role}-${crypto.randomUUID()}`,
          role,
          text,
          streaming: false,
          name,
          ok
        }
      ])
    },
    finishRequest: (requestId, text, cancelled) => {
      if (!awaitingDone.has(requestId)) return
      closeRequest(requestId)
      finishAssistant(requestId, text, cancelled)
    },
    failRequest: (requestId, message) => {
      const prefix = requestId ? `[${requestId}] ` : ""
      setMessages(current => [
        ...current,
        {
          id: `system-${crypto.randomUUID()}`,
          role: "system",
          text: `${prefix}${message}`,
          streaming: false
        }
      ])

      if (awaitingDone.has(requestId)) {
        closeRequest(requestId)
        finishAssistant(requestId, "", false)
      }
    },
    cancelActive: () => {
      const requestId = activeRequest()
      if (!requestId) return undefined

      setActiveRequest(undefined)
      setBusy(false)
      return requestId
    },
    setSystemError: message => {
      setMessages(current => [
        ...current,
        {
          id: `system-${crypto.randomUUID()}`,
          role: "system",
          text: message,
          streaming: false
        }
      ])
    },
    setSystemInfo: message => {
      setMessages(current => [
        ...current,
        {
          id: `info-${crypto.randomUUID()}`,
          role: "info",
          text: message,
          streaming: false
        }
      ])
    },
    clear: () => {
      awaitingDone.clear()
      requestMessages.clear()
      setActiveRequest(undefined)
      setBusy(false)
      setMessages([])
      setJump(false)
      setStats({ used: 0, limit: stats().limit, percent: 0 })
    }
  }
}
