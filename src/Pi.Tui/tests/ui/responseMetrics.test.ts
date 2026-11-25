import { describe, expect, it } from "bun:test"
import type { ChatMessage } from "../../src/state/reducers"
import {
  finishedResponse,
  formatDuration,
  responseSummaryLine,
  streamingResponseId,
  tokenDelta,
  tokensPerSecond
} from "../../src/ui/responseMetrics"

function message(id: string, streaming: boolean): ChatMessage {
  return { id, role: "assistant", text: "resposta", streaming }
}

describe("resumo de tempo e tok/s no chat", () => {
  it("formata tempo com fallback seguro", () => {
    expect(formatDuration(0)).toBe("0 ms")
    expect(formatDuration(750)).toBe("750 ms")
    expect(formatDuration(2000)).toBe("2.0 s")
    expect(formatDuration(-1)).toBe("—")
    expect(formatDuration(Number.NaN)).toBe("—")
  })

  it("calcula delta de tokens sem valores inválidos", () => {
    expect(tokenDelta(1200, 1000)).toBe(200)
    expect(tokenDelta(900, 1000)).toBe(0)
    expect(tokenDelta(1200, undefined)).toBe(0)
    expect(tokenDelta(Number.NaN, 1000)).toBe(0)
  })

  it("calcula tok/s e usa 0.0 quando não há medição", () => {
    expect(tokensPerSecond(200, 4000)).toBe("50.0")
    expect(tokensPerSecond(0, 4000)).toBe("0.0")
    expect(tokensPerSecond(200, 0)).toBe("0.0")
    expect(tokensPerSecond(Number.NaN, 1000)).toBe("0.0")
  })

  it("monta a linha com agente, modelo, tempo e velocidade", () => {
    expect(responseSummaryLine({
      agent: "Leitor",
      model: "qwen3.8-2b-local",
      elapsedMs: 2000,
      tokens: 200
    })).toBe("Leitor · qwen3.8-2b-local · 2.0 s · 100.0 tok/s")

    expect(responseSummaryLine({
      agent: "",
      model: "",
      elapsedMs: 0,
      tokens: 0
    })).toBe("agente · modelo · 0 ms · 0.0 tok/s")
  })

  it("só conclui o resumo depois que o streaming termina", () => {
    const streaming = [message("assistant-1", true)]

    expect(streamingResponseId(streaming)).toBe("assistant-1")
    expect(finishedResponse("assistant-1", streaming)).toBeUndefined()

    const finished = [message("assistant-1", false)]
    expect(finishedResponse("assistant-1", finished)?.id).toBe("assistant-1")

    // Mensagens antigas do snapshot não recebem resumo sem um turno ativo.
    expect(finishedResponse(undefined, finished)).toBeUndefined()
    expect(finishedResponse("assistant-2", finished)).toBeUndefined()
  })
})
