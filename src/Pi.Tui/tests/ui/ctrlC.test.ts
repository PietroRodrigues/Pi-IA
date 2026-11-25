import { describe, expect, it } from "bun:test"
import { ctrlCAction, isExitCommand } from "../../src/ui/ctrlC"

describe("decisão do Ctrl+C", () => {
  it("copia quando há seleção na tela ou no campo", () => {
    expect(ctrlCAction({
      hasScreenSelection: true,
      hasInputSelection: false,
      busy: false
    })).toBe("copy")

    expect(ctrlCAction({
      hasScreenSelection: false,
      hasInputSelection: true,
      busy: true
    })).toBe("copy")
  })

  it("cancela o turno quando ocupado e sem seleção", () => {
    expect(ctrlCAction({
      hasScreenSelection: false,
      hasInputSelection: false,
      busy: true
    })).toBe("cancel")
  })

  it("nunca encerra: sem seleção e livre não faz nada", () => {
    const action = ctrlCAction({
      hasScreenSelection: false,
      hasInputSelection: false,
      busy: false
    })
    expect(action).toBe("none")
    expect(action).not.toBe("exit")
  })
})

describe("saída explícita por comando", () => {
  it("reconhece /sair e /exit em qualquer caixa", () => {
    expect(isExitCommand("/sair")).toBe(true)
    expect(isExitCommand("/EXIT")).toBe(true)
    expect(isExitCommand("  /sair  ")).toBe(true)
  })

  it("não trata outros textos como saída", () => {
    expect(isExitCommand("")).toBe(false)
    expect(isExitCommand("sair")).toBe(false)
    expect(isExitCommand("/sair agora")).toBe(false)
    expect(isExitCommand("/session/new")).toBe(false)
  })
})
