import type { InputRenderable } from "@opentui/core"
import { useKeyboard, useRenderer } from "@opentui/solid"
import { onCleanup, onMount } from "solid-js"
import { BridgeClient } from "../bridge/client"
import type { BridgeOutput } from "../bridge/protocol"
import { getDebugMonitor } from "../debug/monitor"
import { createChatState, type ChatState } from "../state"
import { theme } from "../theme"
import { Composer } from "./Composer"
import { Footer } from "./Footer"
import { MessageList } from "./MessageList"
import { Sidebar } from "./Sidebar"

function handleEvent(event: BridgeOutput, state: ChatState): void {
  switch (event.type) {
    case "ready":
      state.setSession({
        model: event.model,
        agent: event.agent,
        session: event.session,
        cwd: event.cwd,
        branch: event.branch
      })
      state.setStatus("pronto")
      break

    case "context_stats":
      state.setStats({ used: event.used, limit: event.limit, percent: event.percent })
      break

    case "token":
      state.appendToken(event.requestId, event.text)
      break

    case "thought":
      if (state.isActive(event.requestId) && event.text.trim().length > 0) {
        const text = event.text.trim()
        state.addActivity(event.requestId, "thought", text.slice(0, 160))
        state.setStatus(`pensando · ${text.slice(0, 48)}`)
      }
      break

    case "tool_call":
      if (state.isActive(event.requestId)) {
        state.addActivity(event.requestId, "tool", event.input, event.name)
      }
      state.setStatus(`ferramenta · ${event.name}`)
      break

    case "tool_result":
      if (state.isActive(event.requestId)) {
        state.addActivity(event.requestId, "tool", event.text, event.name, event.ok)
      }
      state.setStatus(
        event.ok ? `ferramenta ${event.name} concluída` : `ferramenta ${event.name} falhou`
      )
      break

    case "done":
      if (state.isAwaitingDone(event.requestId)) {
        state.finishRequest(event.requestId, event.text, event.cancelled)
        state.setStatus(event.cancelled ? "cancelado" : "pronto")
      }
      break

    case "error":
      state.failRequest(event.requestId, event.message)
      state.setStatus("erro")
      break
  }
}

export function App() {
  const renderer = useRenderer()
  const state = createChatState()
  const monitor = getDebugMonitor()

  let bridge: BridgeClient | undefined
  let inputRef: InputRenderable | undefined
  let closing = false

  const cancelActive = () => {
    const requestId = state.cancelActive()
    if (!requestId) return false

    bridge?.cancel(requestId)
    state.setStatus("cancelando…")
    inputRef?.focus()
    return true
  }

  const shutdown = async () => {
    if (closing) return
    closing = true

    const requestId = state.cancelActive()
    if (requestId) bridge?.cancel(requestId)

    bridge?.dispose()
    renderer.destroy()
    await renderer.closed
    process.exit(0)
  }

  useKeyboard(key => {
    if (key.ctrl && key.name === "c") {
      if (cancelActive()) return
      void shutdown()
      return
    }

    if (key.name === "escape" || key.name === "esc") {
      cancelActive()
      return
    }

    if (key.ctrl && key.name === "p") {
      inputRef?.focus()
      return
    }

    if (key.ctrl && key.name === "l") {
      state.clear()
      bridge?.clear()
      state.setStatus("contexto limpo")
      inputRef?.focus()
    }
  })

  onMount(() => {
    if (monitor.enabled) {
      state.setSystemInfo(`Modo debug ativo · logs em ${monitor.logDir}`)
      monitor.log("TUI montada")
    }

    bridge = new BridgeClient({
      onEvent: event => handleEvent(event, state),
      onExit: (code, stderrTail) => {
        if (closing) return

        if (code === 0) {
          state.setStatus("backend encerrado")
          return
        }

        state.setStatus(`backend encerrou (${code ?? "sinal"})`)
        if (stderrTail.trim().length > 0) {
          state.setSystemError(stderrTail.slice(-500))
        }
      }
    }, monitor)

    try {
      bridge.start()
    } catch (error) {
      state.setSystemError(error instanceof Error ? error.message : String(error))
      state.setStatus("erro")
    }
  })

  onCleanup(() => bridge?.dispose())

  const submit = (text: string) => {
    if (state.busy()) return

    const requestId = crypto.randomUUID()

    state.addUser(text)
    state.startRequest(requestId)
    state.setStatus("pensando…")
    bridge?.prompt(requestId, text)
  }

  return (
    <box
      style={{
        width: "100%",
        height: "100%",
        flexDirection: "column",
        backgroundColor: theme.background
      }}
    >
      <box style={{ width: "100%", flexGrow: 1, flexDirection: "row", minHeight: 0 }}>
        <box style={{ flexGrow: 1, flexDirection: "column", minWidth: 0 }}>
          <MessageList state={state} onJump={() => state.setJump(false)} />
          <Composer onSubmit={submit} busy={state.busy} onRef={input => (inputRef = input)} />
        </box>

        <Sidebar state={state} />
      </box>

      <Footer state={state} />
    </box>
  )
}
