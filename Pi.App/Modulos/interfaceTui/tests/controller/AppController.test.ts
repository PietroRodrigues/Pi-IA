// Testes do Controller da TUI: traduz eventos, teclado e submissões em
// mudanças no Model. A ponte é falsa — nenhum dotnet é iniciado aqui.
import { describe, expect, it } from "bun:test"
import {
  AppController,
  type ControllerViewPort
} from "../../src/controller/AppController"
import type { BridgeClient, BridgeHandlers } from "../../src/infra/bridge/client"
import type { DebugMonitor } from "../../src/infra/debug/monitor"
import { createChatModel } from "../../src/model/ChatModel"

type Registro = string[]

function bridgeFake(registro: Registro): BridgeClient {
  return {
    start: () => registro.push("bridge.start"),
    prompt: (requestId: string, text: string) => registro.push(`prompt:${requestId}:${text}`),
    cancel: (requestId?: string) => registro.push(`cancel:${requestId ?? "*"}`),
    clear: () => registro.push("clear"),
    dispose: () => registro.push("bridge.dispose")
  } as unknown as BridgeClient
}

const monitorFake = {
  enabled: false,
  logDir: "teste",
  log: () => {},
  stderr: () => {},
  ndjson: () => {},
  build: () => {},
  path: () => "teste"
} as unknown as DebugMonitor

function montar() {
  const registro: Registro = []
  const saidas: number[] = []
  const view: ControllerViewPort = {
    focusInput: () => registro.push("focus"),
    close: async () => {
      registro.push("close")
    }
  }
  let handlers: BridgeHandlers | undefined

  const model = createChatModel()
  const controller = new AppController({
    model,
    view,
    monitor: monitorFake,
    createBridge: recebidos => {
      handlers = recebidos
      return bridgeFake(registro)
    },
    exit: code => {
      saidas.push(code)
    }
  })

  return { controller, model, registro, saidas, handlers: () => handlers! }
}

describe("AppController", () => {
  it("start inicia a ponte e dispose encerra os handlers e a ponte", () => {
    const { controller, registro } = montar()

    controller.start()
    expect(registro).toContain("bridge.start")

    controller.dispose()
    expect(registro).toContain("bridge.dispose")
  })

  it("falha ao iniciar a ponte vira mensagem de sistema e status de erro", () => {
    const registro: Registro = []
    const model = createChatModel()
    const controller = new AppController({
      model,
      view: { focusInput: () => {}, close: async () => {} },
      monitor: monitorFake,
      createBridge: () =>
        ({
          start: () => {
            throw new Error("dll ausente")
          },
          dispose: () => {}
        }) as unknown as BridgeClient
    })

    controller.start()

    expect(model.status()).toBe("erro")
    expect(model.messages().some(mensagem => mensagem.text === "dll ausente")).toBe(true)
    controller.dispose()
    expect(registro).toEqual([])
  })

  it("ready e context_stats atualizam sessão, status e uso de contexto", () => {
    const { controller, model, registro } = montar()
    controller.start()

    controller.handleEvent({
      type: "ready",
      model: "qwen",
      agent: "geral",
      session: "Sessão Pi",
      cwd: "/tmp",
      branch: "main"
    })
    controller.handleEvent({ type: "context_stats", used: 12, limit: 100, percent: 12 })

    expect(model.session()).toEqual({
      model: "qwen",
      agent: "geral",
      session: "Sessão Pi",
      cwd: "/tmp",
      branch: "main"
    })
    expect(model.status()).toBe("pronto")
    expect(model.stats()).toEqual({ used: 12, limit: 100, percent: 12 })
    controller.dispose()
    expect(registro).toContain("bridge.start")
  })

  it("token, thought e ferramentas alimentam o pedido ativo", () => {
    const { controller, model } = montar()
    controller.start()
    controller.submit("oi")
    const requestId = model.activeRequest()!

    controller.handleEvent({ type: "token", requestId, text: "Ol" })
    controller.handleEvent({ type: "token", requestId, text: "á" })
    controller.handleEvent({ type: "thought", requestId, text: "  analisando  " })
    controller.handleEvent({ type: "tool_call", requestId, name: "SearchFiles", input: "nome" })
    controller.handleEvent({
      type: "tool_result",
      requestId,
      name: "SearchFiles",
      ok: true,
      text: "3 arquivos"
    })

    expect(model.messages()[1].text).toBe("Olá")
    expect(model.messages().some(mensagem =>
      mensagem.role === "thought" && mensagem.text === "analisando"
    )).toBe(true)
    expect(model.messages().some(mensagem =>
      mensagem.role === "tool" &&
      mensagem.name === "SearchFiles" &&
      mensagem.text === "3 arquivos" &&
      mensagem.ok === true
    )).toBe(true)
    expect(model.status()).toBe("ferramenta SearchFiles concluída")
    controller.dispose()
  })

  it("tokens e pensamentos de pedidos inativos não entram no Model", () => {
    const { controller, model } = montar()
    controller.start()

    controller.handleEvent({ type: "token", requestId: "alheio", text: "ignorado" })
    controller.handleEvent({ type: "thought", requestId: "alheio", text: "ignorado" })

    expect(model.messages()).toHaveLength(0)
    controller.dispose()
  })

  it("done fecha o pedido; done de pedido alheio é descartado", () => {
    const { controller, model } = montar()
    controller.start()

    controller.handleEvent({ type: "done", requestId: "alheio", text: "x", cancelled: false })
    expect(model.messages()).toHaveLength(0)

    controller.submit("oi")
    const requestId = model.activeRequest()!
    controller.handleEvent({ type: "done", requestId, text: "OK", cancelled: false })

    expect(model.messages()[1].text).toBe("OK")
    expect(model.messages()[1].streaming).toBe(false)
    expect(model.busy()).toBe(false)
    expect(model.status()).toBe("pronto")
    controller.dispose()
  })

  it("done cancelado marca a bolha como cancelada", () => {
    const { controller, model } = montar()
    controller.start()
    controller.submit("oi")
    const requestId = model.activeRequest()!
    controller.cancelActive()

    controller.handleEvent({ type: "done", requestId, text: "", cancelled: true })

    expect(model.messages()[1].text).toBe("[cancelado]")
    expect(model.status()).toBe("cancelado")
    controller.dispose()
  })

  it("error registra a falha e muda o status", () => {
    const { controller, model } = montar()
    controller.start()

    controller.handleEvent({ type: "error", requestId: "r1", message: "boom" })

    expect(model.messages().some(mensagem => mensagem.text === "[r1] boom")).toBe(true)
    expect(model.status()).toBe("erro")
    controller.dispose()
  })

  it("submit envia prompt com requestId e ignora novo submit enquanto ocupado", () => {
    const { controller, model, registro } = montar()
    controller.start()

    controller.submit("primeiro")
    const requestId = model.activeRequest()!
    controller.submit("segundo")

    expect(registro).toContain(`prompt:${requestId}:primeiro`)
    expect(registro.filter(entry => entry.startsWith("prompt:"))).toHaveLength(1)
    expect(model.busy()).toBe(true)
    expect(model.status()).toBe("pensando…")
    controller.dispose()
  })

  it("Esc cancela o pedido e avisa a ponte", () => {
    const { controller, model, registro, saidas } = montar()
    controller.start()
    controller.submit("oi")
    const requestId = model.activeRequest()!

    controller.onKey({ ctrl: false, name: "escape" })

    expect(registro).toContain(`cancel:${requestId}`)
    expect(registro).toContain("focus")
    expect(model.activeRequest()).toBeUndefined()
    expect(model.status()).toBe("cancelando…")
    expect(saidas).toEqual([])
    controller.dispose()
  })

  it("Ctrl+P foca o input e Ctrl+L limpa o contexto", () => {
    const { controller, model, registro } = montar()
    controller.start()
    controller.submit("oi")

    controller.onKey({ ctrl: true, name: "p" })
    controller.onKey({ ctrl: true, name: "l" })

    expect(registro).toContain("focus")
    expect(registro).toContain("clear")
    expect(model.messages()).toEqual([])
    expect(model.status()).toBe("contexto limpo")
    controller.dispose()
  })

  it("Ctrl+C cancela o pedido quando há geração ativa", () => {
    const { controller, model, registro, saidas } = montar()
    controller.start()
    controller.submit("oi")
    const requestId = model.activeRequest()!

    controller.onKey({ ctrl: true, name: "c" })

    expect(registro).toContain(`cancel:${requestId}`)
    expect(saidas).toEqual([])
    controller.dispose()
  })

  it("Ctrl+C ocioso fecha a View e encerra com código 0", async () => {
    const { controller, registro, saidas } = montar()
    controller.start()

    controller.onKey({ ctrl: true, name: "c" })
    await Bun.sleep(0)

    expect(registro).toContain("bridge.dispose")
    expect(registro).toContain("close")
    expect(saidas).toEqual([0])
  })

  it("onExit reflete o código de saída do backend no status", () => {
    const { controller, model, handlers } = montar()
    controller.start()

    handlers().onExit(0, "")
    expect(model.status()).toBe("backend encerrado")

    handlers().onExit(1, "erro grave no backend")
    expect(model.status()).toBe("backend encerrou (1)")
    expect(model.messages().some(mensagem => mensagem.text === "erro grave no backend")).toBe(true)
    controller.dispose()
  })
})
