import { describe, expect, it } from "bun:test"
import { GLYPHS } from "../../src/constants"
import type { ChatMessage } from "../../src/state/reducers"
import {
  footerStatus,
  hasStreamingAssistant,
  inputContextLine,
  inputContextSegments,
  isThinkingTurn,
  thinkingFrame,
  thinkingLabel,
  thinkingText
} from "../../src/ui/chatIndicators"

function assistant(text: string, streaming: boolean): ChatMessage {
  return { id: "assistant-1", role: "assistant", text, streaming }
}

describe("indicador de execução dentro do chat", () => {
  it("anima as reticências em sequência e reinicia o ciclo", () => {
    expect(GLYPHS.thinkingFrames).toEqual([".", "..", "..."])
    expect([0, 1, 2, 3, 4].map(thinkingFrame)).toEqual([".", "..", "...", ".", ".."])
    expect(thinkingFrame(-1)).toBe("...")
  })

  it("monta o rótulo pensando com o frame atual", () => {
    expect(thinkingLabel(0)).toBe("pensando.")
    expect(thinkingLabel(1)).toBe("pensando..")
    expect(thinkingLabel(2)).toBe("pensando...")
  })

  it("ocupa a linha inicial da resposta antes do primeiro token", () => {
    expect(hasStreamingAssistant([assistant("", true)])).toBe(true)
    expect(isThinkingTurn(true, [assistant("", true)])).toBe(true)
    expect(thinkingText(true, [assistant("", true)], 0)).toBe("pensando.")
    expect(thinkingText(true, [assistant("", true)], 2)).toBe("pensando...")
  })

  it("some assim que o texto do assistente começa ou o turno termina", () => {
    const firstToken = [assistant("O", true)]
    expect(isThinkingTurn(true, firstToken)).toBe(false)
    expect(thinkingText(true, firstToken, 0)).toBe("")

    expect(isThinkingTurn(false, [assistant("", true)])).toBe(false)
    expect(thinkingText(false, [assistant("", true)], 0)).toBe("")
    expect(isThinkingTurn(true, [])).toBe(false)
  })

  it("remove pensando do rodapé enquanto o assistente está ocupado", () => {
    expect(footerStatus("pensando…", true)).toBe("")
    expect(footerStatus("pensando…", false)).toBe("pensando…")
    expect(footerStatus("backend encerrado", true)).toBe("backend encerrado")
    expect(footerStatus("", false)).toBe("")
  })

  it("mostra apenas os valores de agente, modelo e status na caixa de entrada", () => {
    expect(inputContextLine("geral", "qwen3.8-2b-local", "pronto", false)).toBe(
      "geral · qwen3.8-2b-local · pronto"
    )
    expect(inputContextLine("", "", "pensando…", true)).toBe("— · —")

    const line = inputContextLine("geral", "qwen3.8-2b-local", "pronto", false)
    expect(line).not.toContain("Agente:")
    expect(line).not.toContain("Modelo:")
  })

  it("separa os segmentos para colorir o agente na View", () => {
    expect(inputContextSegments("geral", "qwen3.8-2b-local", "pronto", false)).toEqual({
      agent: "geral",
      model: "qwen3.8-2b-local",
      status: "pronto"
    })
    expect(inputContextSegments("  ", "  ", "pensando…", true)).toEqual({
      agent: "—",
      model: "—",
      status: ""
    })
  })
})
