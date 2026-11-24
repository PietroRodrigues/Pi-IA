import { describe, expect, it } from "bun:test"
import {
  COMMAND_TYPES,
  EVENT_TYPES,
  createEnvelope,
  encodeCommand,
  parseEnvelope,
  PROTOCOL_VERSION
} from "../../src/protocol/schema"

describe("protocolo v1", () => {
  it("faz roundtrip de um evento com envelope completo", () => {
    const original = createEnvelope(
      EVENT_TYPES.streamDelta,
      { text: "Olá", final: false },
      "corr-1",
      "event-1"
    )

    const parsed = parseEnvelope(JSON.stringify(original))

    expect(parsed).toEqual(original)
    expect(parsed.v).toBe(PROTOCOL_VERSION)
    expect(parsed.type).toBe("stream_delta")
  })

  it("codifica comandos com correlationId", () => {
    const line = encodeCommand(COMMAND_TYPES.promptSubmit, { text: "oi" }, "corr-2")
    const parsed = parseEnvelope(line)

    expect(parsed.correlationId).toBe("corr-2")
    expect(parsed.payload).toEqual({ text: "oi" })
  })

  it("rejeita versão diferente da v1", () => {
    const invalid = { ...createEnvelope(EVENT_TYPES.contextMetrics, { used: 1, limit: 2, percent: 50 }), v: 2 }
    expect(() => parseEnvelope(JSON.stringify(invalid))).toThrow(/Versão do protocolo/)
  })

  it("rejeita tipo desconhecido e JSON corrompido", () => {
    const unknown = createEnvelope("evento_antigo", {})
    expect(() => parseEnvelope(JSON.stringify(unknown))).toThrow(/Tipo desconhecido/)
    expect(() => parseEnvelope("{")).toThrow(/JSON válido/)
  })
})
