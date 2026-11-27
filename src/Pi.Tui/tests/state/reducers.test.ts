import { describe, expect, it } from "bun:test"
import { PROTOCOL } from "../../src/constants"
import { createEnvelope, type EventEnvelope } from "../../src/protocol/schema"
import { initialSessionState, reduceSession, type SessionState } from "../../src/state/reducers"

function event(type: string, payload: unknown, correlationId = ""): EventEnvelope {
  return createEnvelope(type, payload, correlationId) as EventEnvelope
}

function assistantText(state: SessionState, correlationId: string): string {
  return state.messages.find(message => message.id === `assistant-${correlationId}`)?.text ?? ""
}

describe("redutor da sessão", () => {
  it("abre o turno com mensagem do usuário e bolha do assistente", () => {
    const state = reduceSession(initialSessionState, {
      kind: "submit",
      correlationId: "r1",
      text: "olá"
    })

    expect(state.messages.map(message => message.role)).toEqual(["user", "assistant"])
    expect(state.messages[1].streaming).toBe(true)
    expect(state.activeCorrelationId).toBe("r1")
    expect(state.awaitingDone).toContain("r1")
  })

  it("acumula deltas e finaliza com stream_delta final", () => {
    let state = reduceSession(initialSessionState, {
      kind: "submit",
      correlationId: "r1",
      text: "olá"
    })

    state = reduceSession(state, {
      kind: "event",
      event: event(PROTOCOL.eventTypes.streamDelta, { text: "Ol" }, "r1")
    })
    state = reduceSession(state, {
      kind: "event",
      event: event(PROTOCOL.eventTypes.streamDelta, { text: "á" }, "r1")
    })
    state = reduceSession(state, {
      kind: "event",
      event: event(PROTOCOL.eventTypes.streamDelta, { text: "", final: true }, "r1")
    })

    expect(assistantText(state, "r1")).toBe("Olá")
    expect(state.messages[1].streaming).toBe(false)
    expect(state.activeCorrelationId).toBeUndefined()
    expect(state.awaitingDone).not.toContain("r1")
  })

  it("ignora deltas após cancelar, mas fecha a bolha no final", () => {
    let state = reduceSession(initialSessionState, {
      kind: "submit",
      correlationId: "r1",
      text: "olá"
    })

    state = reduceSession(state, { kind: "cancel", correlationId: "r1" })
    state = reduceSession(state, {
      kind: "event",
      event: event(PROTOCOL.eventTypes.streamDelta, { text: "IGNORADO" }, "r1")
    })

    expect(assistantText(state, "r1")).toBe("")

    state = reduceSession(state, {
      kind: "event",
      event: event(PROTOCOL.eventTypes.streamDelta, { text: "", final: true }, "r1")
    })

    expect(assistantText(state, "r1")).toBe("[cancelado]")
    expect(state.activeCorrelationId).toBeUndefined()
  })

  it("registra pedido de aprovação e limpa após a decisão", () => {
    let state = reduceSession(initialSessionState, {
      kind: "submit",
      correlationId: "r1",
      text: "edite arquivo.txt"
    })

    state = reduceSession(state, {
      kind: "event",
      event: event(
        PROTOCOL.eventTypes.toolRequest,
        {
          toolRequestId: "t1",
          name: "edit_file",
          input: "edite arquivo.txt",
          risk: "Write",
          requiresDecision: true
        },
        "r1"
      )
    })

    expect(state.pendingToolRequest?.toolRequestId).toBe("t1")
    expect(state.status).toContain("aprovação")

    state = reduceSession(state, { kind: "decision", toolRequestId: "t1" })
    expect(state.pendingToolRequest).toBeUndefined()
  })

  it("aplica snapshot de contexto e troca mensagens na abertura", () => {
    const snapshot = {
      id: "sessao-1",
      model: "qwen3.8-2b-local",
      agent: "geral",
      cwd: "C:/projeto",
      branch: "main",
      messages: [{ role: "user", text: "oi", timestamp: "2026-01-01T00:00:00Z" }],
      context: { used: 10, limit: 32768, percent: 1 },
      updatedAt: "2026-01-01T00:00:00Z"
    }

    const state = reduceSession(initialSessionState, {
      kind: "event",
      event: event(PROTOCOL.eventTypes.sessionSnapshot, { session: snapshot })
    })

    expect(state.messages).toHaveLength(1)
    expect(state.session.model).toBe("qwen3.8-2b-local")
    expect(state.context.used).toBe(10)
    expect(state.awaitingSnapshot).toBe(false)
  })

  it("transforma erro em mensagem de sistema e fecha o turno ativo", () => {
    let state = reduceSession(initialSessionState, {
      kind: "submit",
      correlationId: "r1",
      text: "oi"
    })

    state = reduceSession(state, {
      kind: "event",
      event: event(PROTOCOL.eventTypes.errorEvent, { code: "llm_failure", message: "falhou" }, "r1")
    })

    expect(state.messages.at(-1)?.text).toBe("[llm_failure] falhou")
    expect(state.activeCorrelationId).toBeUndefined()
    expect(state.status).toBe("erro")
  })
})
