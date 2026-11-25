import { describe, expect, it } from "bun:test"
import {
  commandInputChanged,
  commandInputEnter,
  commandInputEscape
} from "../../src/ui/commandInput"
import { initialCommandMenuState, visibleItems } from "../../src/ui/commandMenuState"

describe("campo de chat que controla o menu de comandos", () => {
  it("abre o menu ao digitar / e mantém o foco no valor do campo", () => {
    const result = commandInputChanged(initialCommandMenuState, "/")

    expect(result.value).toBe("/")
    expect(result.state.open).toBe(true)
    expect(result.state.path).toEqual([])
    expect(visibleItems(result.state).map(item => item.label)).toContain("/providers")
  })

  it("usa o texto depois da barra como filtro", () => {
    let result = commandInputChanged(initialCommandMenuState, "/")
    result = commandInputChanged(result.state, "/local")

    expect(result.state.query).toBe("local")
    expect(visibleItems(result.state).map(item => item.label)).toEqual(["/local-models"])
  })

  it("fecha o menu com Backspace quando o valor deixa de começar com /", () => {
    let result = commandInputChanged(initialCommandMenuState, "/providers")

    result = commandInputChanged(result.state, "/provider")
    expect(result.state.open).toBe(true)

    result = commandInputChanged(result.state, "")
    expect(result.state.open).toBe(false)
    expect(result.state.path).toEqual([])
    expect(result.state.query).toBe("")
    expect(result.value).toBe("")
  })

  it("Enter navega no menu sem gerar prompt e reinicia o filtro ao trocar de nível", () => {
    let result = commandInputChanged(initialCommandMenuState, "/prov")
    result = commandInputEnter(result.state, "/prov")

    expect(result.state.path).toEqual(["providers"])
    expect(result.value).toBe("/")

    result = commandInputChanged(result.state, "/list")
    expect(result.state.query).toBe("list")

    result = commandInputEnter(result.state, "/list")
    expect(result.state.path).toEqual(["providers", "list"])
    expect(result.value).toBe("/")
  })

  it("Enter em uma folha fecha o menu e limpa o campo", () => {
    let result = commandInputChanged(initialCommandMenuState, "/prov")
    result = commandInputEnter(result.state, "/prov")
    result = commandInputChanged(result.state, "/list")
    result = commandInputEnter(result.state, "/list")

    result = commandInputChanged(result.state, "/nvidia")
    result = commandInputEnter(result.state, "/nvidia")

    expect(result.state.open).toBe(false)
    expect(result.state.provider).toBe("NVIDIA")
    expect(result.value).toBe("")
  })

  it("Esc no menu volta um nível e devolve o campo para /", () => {
    let result = commandInputChanged(initialCommandMenuState, "/prov")
    result = commandInputEnter(result.state, "/prov")

    result = commandInputEscape(result.state, "/")

    expect(result.state.path).toEqual([])
    expect(result.state.open).toBe(true)
    expect(result.value).toBe("/")
  })

  it("não mexe no estado quando o campo não é um comando", () => {
    const result = commandInputChanged(initialCommandMenuState, "olá")

    expect(result.state).toBe(initialCommandMenuState)
    expect(result.value).toBe("olá")
  })
})
