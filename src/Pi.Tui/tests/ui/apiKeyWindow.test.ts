import { describe, expect, it } from "bun:test"
import type { CommandMenuState } from "../../src/ui/commandMenuState"
import {
  initialCommandMenuState,
  reduceCommandMenu,
  visibleItems
} from "../../src/ui/commandMenuState"

function open(query = "", base: CommandMenuState = initialCommandMenuState): CommandMenuState {
  return reduceCommandMenu(base, { kind: "type", value: query })
}

function goToConnect(base: CommandMenuState = initialCommandMenuState): CommandMenuState {
  let state = reduceCommandMenu(open("prov", base), { kind: "enter" })
  state = reduceCommandMenu(state, { kind: "down" })
  state = reduceCommandMenu(state, { kind: "enter" })
  state = reduceCommandMenu(state, { kind: "down" })
  return reduceCommandMenu(state, { kind: "enter" })
}

describe("janela visual de API key", () => {
  it("abre com o título do provedor ao selecionar em /providers/connect", () => {
    const state = goToConnect()

    expect(state.apiKey).toMatchObject({
      provider: "NVIDIA",
      providerId: "nvidia",
      value: ""
    })
    expect(state.apiKey?.returnPath).toEqual(["providers", "connect"])
    expect(visibleItems(state)).toEqual([])
  })

  it("Submit marca o provedor como conectado apenas no estado visual e fecha a janela", () => {
    let state = goToConnect()
    state = reduceCommandMenu(state, { kind: "apiKeyType", value: "chave-de-demonstracao" })
    expect(state.apiKey?.value).toBe("chave-de-demonstracao")

    state = reduceCommandMenu(state, { kind: "apiKeySubmit" })

    expect(state.apiKey).toBeUndefined()
    expect(state.path).toEqual(["providers", "connect"])
    expect(state.open).toBe(true)
    expect(state.connectedProviders).toEqual(["nvidia"])
    expect(state.provider).toBe("NVIDIA")
    expect(visibleItems(state).find(item => item.id === "nvidia")?.active).toBe(true)
  })

  it("Enter confirma a janela quando o estado local está aberto", () => {
    let state = goToConnect()
    state = reduceCommandMenu(state, { kind: "enter" })

    expect(state.apiKey).toBeUndefined()
    expect(state.connectedProviders).toContain("nvidia")
  })

  it("Esc cancela sem conectar e volta para /providers/connect", () => {
    let state = goToConnect()
    state = reduceCommandMenu(state, { kind: "apiKeyCancel" })

    expect(state.apiKey).toBeUndefined()
    expect(state.path).toEqual(["providers", "connect"])
    expect(state.connectedProviders).toEqual([])
  })

  it("não duplica provedores conectados ao confirmar duas vezes", () => {
    const first = reduceCommandMenu(goToConnect(), { kind: "apiKeySubmit" })
    const closed = reduceCommandMenu(first, { kind: "close" })
    const reopened = goToConnect(closed)
    const confirmed = reduceCommandMenu(reopened, { kind: "apiKeySubmit" })

    expect(confirmed.connectedProviders).toEqual(["nvidia"])
  })
})
