// A tela principal. Liga eventos da ponte ao estado, cuida do teclado e
// concentra o encerramento em um único caminho.
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
    monitor.log("encerrando TUI")

    const requestId = state.cancelActive()
    if (requestId) bridge?.cancel(requestId)

    try {
      bridge?.dispose()
      renderer.destroy()
      // closed é necessário antes de process.exit(); o timeout evita travar
      // caso a cena nativa não conclua a restauração do terminal.
      await Promise.race([
        renderer.closed.catch(() => undefined),
        new Promise<void>(resolve => setTimeout(resolve, 1000))
      ])
      monitor.log("TUI encerrada")
    } catch (error) {
      monitor.log("falha no encerramento", {
        message: error instanceof Error ? error.message : String(error)
      })
    } finally {
      process.exit(0)
    }
  }

  // Sinais do sistema — inclui o Ctrl+C que o VS Code envia no Stop.
  const onExitSignal = () => {
    void shutdown()
  }

  const EXIT_SIGNALS = ["SIGINT", "SIGTERM", "SIGHUP", "SIGBREAK"] as const

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
    for (const signal of EXIT_SIGNALS) {
      process.on(signal, onExitSignal)
    }

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

  onCleanup(() => {
    for (const signal of EXIT_SIGNALS) {
      process.off(signal, onExitSignal)
    }
    bridge?.dispose()
  })

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
      <box style={{ width: "100%", flexGrow: 1, flexDirection: "column", minHeight: 0 }}>
        <MessageList state={state} onJump={() => state.setJump(false)} />
        <Composer onSubmit={submit} busy={state.busy} onRef={input => (inputRef = input)} />
      </box>

      <Footer state={state} />
    </box>
  )
}
