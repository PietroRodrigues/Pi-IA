// View raiz + controlador da TUI. Mantém a topologia que já funciona: a TUI é
// dona do terminal e inicia o Pi.Host em modo --bridge stdio.
import type { InputRenderable } from "@opentui/core"
import { useKeyboard, useRenderer } from "@opentui/solid"
import { createEffect, createSignal, onCleanup, onMount, Show } from "solid-js"
import {
  BridgeTransport,
  type BridgeHandlers
} from "../protocol/transport"
import { EVENT_TYPES, type EventEnvelope, type ProtocolMessage } from "../protocol/schema"
import { createSessionStore, type SessionStore } from "../state/sessionStore"
import { commandInputChanged, commandInputEnter, commandInputEscape, type CommandInputResult } from "./commandInput"
import { ApprovalModal } from "./ApprovalModal"
import { copyTextToClipboard } from "./clipboard"
import { ChatView } from "./ChatView"
import { ctrlCAction, isExitCommand } from "./ctrlC"
import { CommandMenu } from "./CommandMenu"
import {
  initialCommandMenuState,
  reduceCommandMenu,
  type CommandMenuAction,
  type CommandMenuState
} from "./commandMenuState"
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
  onSigint?: () => void
}

export class TuiController {
  private readonly store: SessionStore
  private readonly view: ControllerViewPort
  private readonly logger: NonNullable<TuiControllerOptions["logger"]>
  private readonly createTransport: NonNullable<TuiControllerOptions["createTransport"]>
  private readonly exit: (code: number) => void
  private readonly onSigint: () => void

  private transport: BridgeTransport | undefined
  private closing = false

  constructor(options: TuiControllerOptions) {
    this.store = options.store
    this.view = options.view
    this.logger = options.logger ?? { log: () => {} }
    this.createTransport =
      options.createTransport ?? (handlers => new BridgeTransport(handlers))
    this.exit = options.exit ?? (code => process.exit(code))
    this.onSigint = options.onSigint ?? (() => {})
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

  // Saída explícita: chamada apenas pelo comando /sair ou /exit.
  requestExit(): void {
    void this.shutdown()
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

  private readonly onExitSignal = (signal?: NodeJS.Signals): void => {
    // Ctrl+C nunca encerra: SIGINT segue a mesma decisão do handler de teclado.
    if (signal === "SIGINT") {
      this.onSigint()
      return
    }
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

  // Menu "/" puramente visual: estado local, sem tocar no host nem no store.
  const [menuState, setMenuState] = createSignal<CommandMenuState>(initialCommandMenuState)
  const menuOpen = () => menuState().open

  const sendMenuAction = (action: CommandMenuAction) => {
    setMenuState(current => reduceCommandMenu(current, action))
  }

  const applyInputResult = (result: CommandInputResult) => {
    setMenuState(result.state)
    if (inputRef && inputRef.value !== result.value) inputRef.value = result.value
  }

  // Um único campo de digitação: "/" abre/filtra; apagar o "/" fecha o menu.
  const onInputValue = (value: string) => {
    applyInputResult(commandInputChanged(menuState(), value))
  }

  const onSubmit = (text: string) => {
    // Saída é só por comando explícito: nunca vai para o host como prompt.
    if (isExitCommand(inputRef?.value ?? text)) {
      setMenuState(current => ({
        ...current,
        open: false,
        path: [],
        query: "",
        index: 0
      }))
      if (inputRef) inputRef.value = ""
      controller.requestExit()
      return
    }

    const wasOpen = menuOpen()
    const result = commandInputEnter(menuState(), inputRef?.value ?? text)
    applyInputResult(result)

    if (!wasOpen && text.length > 0) {
      controller.submit(text)
      if (inputRef) inputRef.value = ""
    }
  }

  let apiKeyWasOpen = false
  createEffect(() => {
    const open = menuState().apiKey !== undefined
    if (open === apiKeyWasOpen) return
    apiKeyWasOpen = open
    if (open) inputRef?.blur()
    else inputRef?.focus()
  })

  const viewPort: ControllerViewPort = {
    focusInput: () => {
      if (!menuState().apiKey) inputRef?.focus()
    },
    close: async () => {
      renderer.destroy()
      await Promise.race([
        renderer.closed.catch(() => undefined),
        new Promise<void>(resolve => setTimeout(resolve, 1000))
      ])
    }
  }

  const controller = new TuiController({
    store,
    view: viewPort,
    onSigint: () => handleCtrlC()
  })

  const [copyNotice, setCopyNotice] = createSignal("")
  let noticeTimer: ReturnType<typeof setTimeout> | undefined
  let lastCtrlCAt = 0

  const showNotice = (message: string) => {
    setCopyNotice(message)
    if (noticeTimer) clearTimeout(noticeTimer)
    noticeTimer = setTimeout(() => setCopyNotice(""), 1800)
  }

  function handleCtrlC(): void {
    // Teclado e SIGINT podem chegar juntos do mesmo pressionamento.
    const now = Date.now()
    if (now - lastCtrlCAt < 80) return
    lastCtrlCAt = now

    const readScreen = () => {
      try {
        return renderer.getSelection()?.getSelectedText() ?? ""
      } catch {
        return ""
      }
    }
    const readInput = () => {
      try {
        return inputRef?.getSelectedText() ?? ""
      } catch {
        return ""
      }
    }

    const screenText = readScreen()
    const inputText = readInput()
    const action = ctrlCAction({
      hasScreenSelection: screenText.trim().length > 0,
      hasInputSelection: inputText.trim().length > 0,
      busy: store.busy()
    })

    if (action === "copy") {
      const text = screenText.trim().length > 0 ? screenText : inputText
      void copyTextToClipboard(text, {
        platform: () => process.platform,
        osc52: value => {
          try {
            return renderer.copyToClipboardOSC52(value)
          } catch {
            return false
          }
        }
      }).then(copied => {
        showNotice(copied ? "Copiado!" : "Não foi possível copiar")
        if (copied) {
          try {
            renderer.clearSelection()
          } catch {
            // Seleção já foi liberada pelo terminal.
          }
        }
      })
      return
    }

    if (action === "cancel") {
      controller.cancelActive()
      showNotice("Turno cancelado")
      return
    }

    showNotice("Use /sair para encerrar")
  }

  useKeyboard(key => {
    if (key.ctrl && key.name === "c") {
      key.preventDefault()
      handleCtrlC()
      return
    }

    if (menuOpen()) {
      if (menuState().apiKey) {
        if (key.name === "escape" || key.name === "esc") {
          key.preventDefault()
          applyInputResult(
            commandInputEscape(menuState(), inputRef?.value ?? "")
          )
        }
        return
      }

      if (key.name === "up" || key.name === "down") {
        key.preventDefault()
        sendMenuAction({ kind: key.name === "up" ? "up" : "down" })
        return
      }

      if (key.name === "escape" || key.name === "esc") {
        key.preventDefault()
        applyInputResult(commandInputEscape(menuState(), inputRef?.value ?? ""))
        return
      }

      return
    }

    controller.onKey(key)
  })
  onMount(() => controller.start())
  onCleanup(() => {
    if (noticeTimer) clearTimeout(noticeTimer)
    controller.dispose()
  })

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
        <Show when={menuOpen()}>
          <CommandMenu state={menuState()} onAction={sendMenuAction} />
        </Show>
        <InputBar
          store={store}
          onSubmit={onSubmit}
          onValueChange={onInputValue}
          onRef={input => (inputRef = input)}
        />
      </box>

      <StatusBar store={store} notice={copyNotice()} />
      <ApprovalModal store={store} />
    </box>
  )
}
