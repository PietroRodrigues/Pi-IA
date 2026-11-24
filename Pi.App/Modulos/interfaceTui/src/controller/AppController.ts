// Controller da TUI: concentra as decisões que antes viviam no App.
// Ele traduz eventos da ponte, teclado e submissões em mudanças no Model;
// a View apenas renderiza o Model e delega a interação para cá.
import { BridgeClient, type BridgeHandlers } from "../infra/bridge/client"
import type { BridgeOutput } from "../infra/bridge/protocol"
import { getDebugMonitor, type DebugMonitor } from "../infra/debug/monitor"
import type { ChatModel, ContextStats, SessionInfo } from "../model/ChatModel"

/** Tecla no formato mínimo usado pelo controller (structurally compatível com a OpenTUI). */
export interface KeyPress {
  readonly ctrl: boolean
  readonly name?: string
}

/** Porta da View usada pelo controller: focar o input e encerrar o renderer. */
export interface ControllerViewPort {
  focusInput(): void
  close(): Promise<void>
}

export interface AppControllerOptions {
  model: ChatModel
  view: ControllerViewPort
  monitor?: DebugMonitor
  createBridge?: (handlers: BridgeHandlers, monitor: DebugMonitor) => BridgeClient
  /** Porta de saída injetável para os testes não encerrarem o runner. */
  exit?: (code: number) => void
}

const EXIT_SIGNALS = ["SIGINT", "SIGTERM", "SIGHUP", "SIGBREAK"] as const

export class AppController {
  private readonly model: ChatModel
  private readonly view: ControllerViewPort
  private readonly monitor: DebugMonitor
  private readonly createBridge: NonNullable<AppControllerOptions["createBridge"]>
  private readonly exit: (code: number) => void

  private bridge: BridgeClient | undefined
  private closing = false

  constructor(options: AppControllerOptions) {
    this.model = options.model
    this.view = options.view
    this.monitor = options.monitor ?? getDebugMonitor()
    this.createBridge = options.createBridge ?? ((handlers, monitor) => new BridgeClient(handlers, monitor))
    this.exit = options.exit ?? (code => process.exit(code))
  }

  start(): void {
    for (const signal of EXIT_SIGNALS) {
      process.on(signal, this.onExitSignal)
    }

    if (this.monitor.enabled) {
      this.model.setSystemInfo(`Modo debug ativo · logs em ${this.monitor.logDir}`)
      this.monitor.log("TUI montada")
    }

    this.bridge = this.createBridge(
      {
        onEvent: event => this.handleEvent(event),
        onExit: (code, stderrTail) => this.handleExit(code, stderrTail)
      },
      this.monitor
    )

    try {
      this.bridge.start()
    } catch (error) {
      this.model.setSystemError(error instanceof Error ? error.message : String(error))
      this.model.setStatus("erro")
    }
  }

  dispose(): void {
    for (const signal of EXIT_SIGNALS) {
      process.off(signal, this.onExitSignal)
    }
    this.bridge?.dispose()
  }

  handleEvent(event: BridgeOutput): void {
    switch (event.type) {
      case "ready": {
        const session: SessionInfo = {
          model: event.model,
          agent: event.agent,
          session: event.session,
          cwd: event.cwd,
          branch: event.branch
        }
        this.model.setSession(session)
        this.model.setStatus("pronto")
        break
      }

      case "context_stats": {
        const stats: ContextStats = {
          used: event.used,
          limit: event.limit,
          percent: event.percent
        }
        this.model.setStats(stats)
        break
      }

      case "token":
        this.model.appendToken(event.requestId, event.text)
        break

      case "thought":
        if (this.model.isActive(event.requestId) && event.text.trim().length > 0) {
          const text = event.text.trim()
          this.model.addActivity(event.requestId, "thought", text.slice(0, 160))
          this.model.setStatus(`pensando · ${text.slice(0, 48)}`)
        }
        break

      case "tool_call":
        if (this.model.isActive(event.requestId)) {
          this.model.addActivity(event.requestId, "tool", event.input, event.name)
        }
        this.model.setStatus(`ferramenta · ${event.name}`)
        break

      case "tool_result":
        if (this.model.isActive(event.requestId)) {
          this.model.addActivity(event.requestId, "tool", event.text, event.name, event.ok)
        }
        this.model.setStatus(
          event.ok ? `ferramenta ${event.name} concluída` : `ferramenta ${event.name} falhou`
        )
        break

      case "done":
        if (this.model.isAwaitingDone(event.requestId)) {
          this.model.finishRequest(event.requestId, event.text, event.cancelled)
          this.model.setStatus(event.cancelled ? "cancelado" : "pronto")
        }
        break

      case "error":
        this.model.failRequest(event.requestId, event.message)
        this.model.setStatus("erro")
        break
    }
  }

  onKey(key: KeyPress): void {
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
    if (this.model.busy()) return

    const requestId = crypto.randomUUID()

    this.model.addUser(text)
    this.model.startRequest(requestId)
    this.model.setStatus("pensando…")
    this.bridge?.prompt(requestId, text)
  }

  cancelActive(): boolean {
    const requestId = this.model.cancelActive()
    if (!requestId) return false

    this.bridge?.cancel(requestId)
    this.model.setStatus("cancelando…")
    this.view.focusInput()
    return true
  }

  clearContext(): void {
    this.model.clear()
    this.bridge?.clear()
    this.model.setStatus("contexto limpo")
    this.view.focusInput()
  }

  private handleExit(code: number | null, stderrTail: string): void {
    if (this.closing) return

    if (code === 0) {
      this.model.setStatus("backend encerrado")
      return
    }

    this.model.setStatus(`backend encerrou (${code ?? "sinal"})`)
    if (stderrTail.trim().length > 0) {
      this.model.setSystemError(stderrTail.slice(-500))
    }
  }

  private readonly onExitSignal = (): void => {
    void this.shutdown()
  }

  private async shutdown(): Promise<void> {
    if (this.closing) return
    this.closing = true
    this.monitor.log("encerrando TUI")

    const requestId = this.model.cancelActive()
    if (requestId) this.bridge?.cancel(requestId)

    try {
      this.bridge?.dispose()
      await this.view.close()
      this.monitor.log("TUI encerrada")
    } catch (error) {
      this.monitor.log("falha no encerramento", {
        message: error instanceof Error ? error.message : String(error)
      })
    } finally {
      this.exit(0)
    }
  }
}
