// Testes do Model da TUI: estado reativo e regras do pedido ativo.
// Não usam ponte, renderer nem filesystem — só os sinais do Solid.
import { describe, expect, it } from "bun:test"
import { createChatModel } from "../../src/model/ChatModel"

describe("ChatModel", () => {
  it("abre o pedido com mensagem do usuário e bolha do assistente", () => {
    const model = createChatModel()

    model.addUser("oi")
    model.startRequest("r1")

    expect(model.messages().map(mensagem => mensagem.role)).toEqual(["user", "assistant"])
    expect(model.messages()[0].text).toBe("oi")
    expect(model.messages()[1].streaming).toBe(true)
    expect(model.activeRequest()).toBe("r1")
    expect(model.isActive("r1")).toBe(true)
    expect(model.isAwaitingDone("r1")).toBe(true)
    expect(model.busy()).toBe(true)
  })

  it("acumula tokens e atividades apenas do pedido ativo", () => {
    const model = createChatModel()
    model.startRequest("r1")

    model.appendToken("r1", "Ol")
    model.appendToken("r1", "á")
    model.addActivity("r1", "thought", "pensando")
    model.addActivity("r1", "tool", "arquivos", "SearchFilesTool", true)

    expect(model.messages()[0].text).toBe("Olá")
    expect(model.messages()[0].streaming).toBe(true)
    expect(model.messages().map(mensagem => mensagem.role)).toEqual([
      "assistant",
      "thought",
      "tool"
    ])
    expect(model.messages()[2].name).toBe("SearchFilesTool")
    expect(model.messages()[2].ok).toBe(true)

    model.appendToken("outro", "ignorado")
    model.addActivity("outro", "tool", "ignorado")
    expect(model.messages().some(mensagem => mensagem.text.includes("ignorado"))).toBe(false)
  })

  it("cancelar ignora eventos novos mas aceita o done para fechar a bolha", () => {
    const model = createChatModel()
    model.startRequest("r1")
    model.appendToken("r1", "parcial")

    expect(model.cancelActive()).toBe("r1")
    expect(model.isActive("r1")).toBe(false)
    expect(model.isAwaitingDone("r1")).toBe(true)
    expect(model.busy()).toBe(false)

    model.appendToken("r1", "ignorado")
    model.addActivity("r1", "thought", "ignorado")
    model.finishRequest("r1", "parcial", true)

    expect(model.messages()).toHaveLength(1)
    expect(model.messages()[0].text).toBe("parcial\n\n[cancelado]")
    expect(model.messages()[0].streaming).toBe(false)
    expect(model.isAwaitingDone("r1")).toBe(false)
  })

  it("erro do backend vira mensagem de sistema e encerra o pedido", () => {
    const model = createChatModel()
    model.startRequest("r1")

    model.failRequest("r1", "boom")

    expect(
      model.messages().some(mensagem =>
        mensagem.role === "system" && mensagem.text === "[r1] boom"
      )
    ).toBe(true)
    expect(model.isAwaitingDone("r1")).toBe(false)
    expect(model.busy()).toBe(false)
  })

  it("finishRequest ignora um requestId desconhecido", () => {
    const model = createChatModel()

    model.finishRequest("inexistente", "texto", false)

    expect(model.messages()).toHaveLength(0)
    expect(model.status()).toBe("conectando…")
  })

  it("clear zera mensagens, contexto, pedido e jump", () => {
    const model = createChatModel()
    model.startRequest("r1")
    model.setStats({ used: 10, limit: 100, percent: 10 })
    model.setJump(true)

    model.clear()

    expect(model.messages()).toEqual([])
    expect(model.stats()).toEqual({ used: 0, limit: 100, percent: 0 })
    expect(model.jump()).toBe(false)
    expect(model.activeRequest()).toBeUndefined()
    expect(model.busy()).toBe(false)
  })

  it("setSession, setStatus e mensagens de sistema/informação", () => {
    const model = createChatModel()

    model.setSession({ model: "m", agent: "a", session: "s", cwd: "c", branch: "b" })
    model.setStatus("pronto")
    model.setSystemInfo("modo debug")
    model.setSystemError("falha")

    expect(model.session().model).toBe("m")
    expect(model.session().branch).toBe("b")
    expect(model.status()).toBe("pronto")
    expect(model.messages().map(mensagem => mensagem.role)).toEqual(["info", "system"])
  })
})
