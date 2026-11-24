// View raiz + controlador da TUI. Mantém a topologia que já funciona: a TUI é
// dona do terminal e inicia o Pi.Host em modo --bridge stdio.
import type { InputRenderable } from "@opentui/core"
import { useKeyboard, useRenderer } from "@opentui/solid"
import { onCleanup, onMount } from "solid-js"
import {
  BridgeTransport,
  type BridgeHandlers
} from "../protocol/transport"
import { EVENT_TYPES, type EventEnvelope, type ProtocolMessage } from "../protocol/schema"
import { createSessionStore, type SessionStore } from "../state/sessionStore"
import { ApprovalModal } from "./ApprovalModal"
import { ChatView } from "./ChatView"
import { InputBar } from "./InputBar"
import { StatusBar } from "./StatusBar"

const EXIT_SIGNALS = ["SIGINT", "SIGTERM", "SIGHUP", "SIGBREAK"] as const

export interface KeyPress {
  readonly ctrl: boolean
  readonly name?: string
}

export interface ControllerViewPort {
  focusInput(): void
  close(): Promise<void>
}

export interface TuiControllerOptions {
  store: SessionStore
  view: ControllerViewPort
  logger?: { log: (message: string, extra?: Record<string, unknown>) => void }
  createTransport?: (handlers: BridgeHandlers) => BridgeTransport
  exit?: (code: number) => void
}

export class TuiController {
  private readonly store: SessionStore
  private readonly view: ControllerViewPort
  private readonly logger: NonNullable<TuiControllerOptions["logger"]>
  private readonly createTransport: NonNullable<TuiControllerOptions["createTransport"]>
  private readonly exit: (code: number) => void

  private transport: BridgeTransport | undefined
  private closing = false

  constructor(options: TuiControllerOptions) {
    this.store = options.store
    this.view = options.view
    this.logger = options.logger ?? { log: () => {} }
    this.createTransport =
      options.createTransport ?? (handlers => new BridgeTransport(handlers))
    this.exit = options.exit ?? (code => process.exit(code))
  }

  start(): void {
    for (const signal of EXIT_SIGNALS) {
      process.on(signal, this.onExitSignal)
    }

    this.logger.log("TUI montada")

    this.transport = this.createTransport({
      onEvent: event => this.handleEvent(event),
      onExit: (code, stderrTail) => this.handleExit(code, stderrTail)
    })

    try {
      this.transport.start()
    } catch (error) {
      this.store.dispatch({
        kind: "system",
        message: error instanceof Error ? error.message : String(error)
      })
      this.store.dispatch({ kind: "status", status: "erro" })
    }
  }

  dispose(): void {
    for (const signal of EXIT_SIGNALS) {
      process.off(signal, this.onExitSignal)
    }
    this.transport?.dispose()
  }

  handleEvent(event: ProtocolMessage): void {
    const eventTypes = new Set<string>(Object.values(EVENT_TYPES))
    if (!eventTypes.has(event.type)) return

    if (event.type === EVENT_TYPES.errorEvent) {
      this.logger.log("erro do backend", { ...event.payload })
    }

    this.store.applyEvent(event as EventEnvelope)
  }

  onKey(key: KeyPress): void {
    const pending = this.store.pendingToolRequest()

    if (pending) {
      if (key.name === "y" || key.name === "Y") {
        this.decide(true)
        return
      }

      if (key.name === "n" || key.name === "N" || key.name === "escape" || key.name === "esc") {
        this.decide(false)
        return
      }
    }

    if (key.ctrl && key.name === "c") {
      if (this.cancelActive()) return
      void this.shutdown()
      return
    }

    if (key.name === "escape" || key.name === "esc") {
      this.cancelActive()
      return
    }

    if (key.ctrl && key.name === "p") {
      this.view.focusInput()
      return
    }

    if (key.ctrl && key.name === "l") {
      this.clearContext()
    }
  }

  submit(text: string): void {
    if (this.store.busy()) return

    const correlationId = crypto.randomUUID()
    this.store.dispatch({ kind: "submit", correlationId, text })
    this.transport?.prompt(text, correlationId)
  }

  cancelActive(): boolean {
    const correlationId = this.store.activeRequest()
    if (!correlationId) return false

    this.store.dispatch({ kind: "cancel", correlationId })
    this.transport?.interrupt(correlationId)
    this.view.focusInput()
    return true
  }

  clearContext(): void {
    this.store.dispatch({ kind: "clear" })
    this.transport?.clear(crypto.randomUUID())
    this.view.focusInput()
  }

  decide(approved: boolean): void {
    const pending = this.store.pendingToolRequest()
    if (!pending) return

    this.transport?.decide(
      pending.toolRequestId,
      approved,
      this.store.activeRequest() ?? "",
      approved ? "aprovado pelo usuário" : "negado pelo usuário"
    )
    this.store.dispatch({ kind: "decision", toolRequestId: pending.toolRequestId })
    this.view.focusInput()
  }

  private handleExit(code: number | null, stderrTail: string): void {
    if (this.closing) return

    if (code === 0) {
      this.store.dispatch({ kind: "status", status: "backend encerrado" })
      return
    }

    this.store.dispatch({ kind: "status", status: `backend encerrou (${code ?? "sinal"})` })
    if (stderrTail.trim().length > 0) {
      this.store.dispatch({
        kind: "system",
        message: stderrTail.slice(-500),
        role: "system"
      })
    }
  }

  private readonly onExitSignal = (): void => {
    void this.shutdown()
  }

  private async shutdown(): Promise<void> {
    if (this.closing) return
    this.closing = true
    this.logger.log("encerrando TUI")

    const correlationId = this.store.activeRequest()
    if (correlationId) this.transport?.interrupt(correlationId, "shutdown")

    try {
      this.transport?.dispose()
      await this.view.close()
      this.logger.log("TUI encerrada")
    } catch (error) {
      this.logger.log("falha no encerramento", {
        message: error instanceof Error ? error.message : String(error)
      })
    } finally {
      this.exit(0)
    }
  }
}

export function App() {
  const renderer = useRenderer()
  const store = createSessionStore()

  let inputRef: InputRenderable | undefined

  const viewPort: ControllerViewPort = {
    focusInput: () => inputRef?.focus(),
    close: async () => {
      renderer.destroy()
      await Promise.race([
        renderer.closed.catch(() => undefined),
        new Promise<void>(resolve => setTimeout(resolve, 1000))
      ])
    }
  }

  const controller = new TuiController({ store, view: viewPort })

  useKeyboard(key => controller.onKey(key))
  onMount(() => controller.start())
  onCleanup(() => controller.dispose())

  return (
    <box
      style={{
        width: "100%",
        height: "100%",
        flexDirection: "column",
        backgroundColor: "#0A0A0A"
      }}
    >
      <box style={{ width: "100%", flexGrow: 1, flexDirection: "column", minHeight: 0 }}>
        <ChatView store={store} onJump={() => store.dispatch({ kind: "jump", jump: false })} />
        <InputBar
          store={store}
          onSubmit={text => controller.submit(text)}
          onRef={input => (inputRef = input)}
        />
      </box>

      <StatusBar store={store} />
      <ApprovalModal store={store} />
    </box>
  )
}
