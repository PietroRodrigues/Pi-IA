import { describe, expect, it } from "bun:test"
import type { ChildProcessWithoutNullStreams } from "node:child_process"
import { join } from "node:path"
import { createEnvelope, EVENT_TYPES, type ProtocolMessage } from "../../src/protocol/schema"
import { BridgeTransport } from "../../src/protocol/transport"
import type { FileLogger } from "../../src/telemetry/fileLogger"

class FakeStream {
  private readonly listeners: Array<(chunk: string) => void> = []

  setEncoding(): void {}

  on(_event: string, listener: (chunk: string) => void): this {
    this.listeners.push(listener)
    return this
  }

  emitData(chunk: string): void {
    for (const listener of this.listeners) listener(chunk)
  }
}

class FakeChild {
  pid = 4242
  exitCode: number | null = null
  signalCode: string | null = null
  stdout = new FakeStream()
  stderr = new FakeStream()
  stdin = {
    writes: [] as string[],
    write(value: string): boolean {
      this.writes.push(value)
      return true
    },
    end(): void {}
  }

  private readonly listeners = new Map<string, Array<(...args: never[]) => void>>()

  on(event: string, listener: (...args: never[]) => void): this {
    const current = this.listeners.get(event) ?? []
    current.push(listener)
    this.listeners.set(event, current)
    return this
  }

  kill(): boolean {
    return true
  }
}

const logger = {
  enabled: false,
  logDir: "teste",
  log: () => {},
  stderr: () => {},
  ndjson: () => {}
} as unknown as FileLogger

function makeTransport(child: FakeChild, events: ProtocolMessage[]) {
  return new BridgeTransport(
    { onEvent: event => events.push(event), onExit: () => {} },
    logger,
    () => child as unknown as ChildProcessWithoutNullStreams
  )
}

describe("transporte NDJSON", () => {
  it("remonta eventos separados em vários chunks", () => {
    const events: ProtocolMessage[] = []
    const transport = makeTransport(new FakeChild(), events)

    const first = JSON.stringify(createEnvelope(EVENT_TYPES.streamDelta, { text: "Ol" }, "r1"))
    const second = JSON.stringify(
      createEnvelope(EVENT_TYPES.streamDelta, { text: "á", final: true }, "r1")
    )

    transport.receive(`${first.slice(0, 20)}`)
    transport.receive(`${first.slice(20)}\n${second}\n`)

    expect(events).toHaveLength(2)
    expect(events[0].payload).toEqual({ text: "Ol" })
    expect(events[1].payload).toEqual({ text: "á", final: true })
  })

  it("converte linha corrompida em error_event sem derrubar o parser", () => {
    const events: ProtocolMessage[] = []
    const transport = makeTransport(new FakeChild(), events)

    transport.receive("linha-invalida\n")

    expect(events).toHaveLength(1)
    expect(events[0].type).toBe(EVENT_TYPES.errorEvent)
  })

  it("envia prompt_submit com correlationId no stdin do backend", () => {
    const previous = process.env.PI_BACKEND_DLL
    process.env.PI_BACKEND_DLL = join(import.meta.dir, "..", "..", "package.json")

    try {
      const child = new FakeChild()
      const transport = makeTransport(child, [])
      transport.start()
      transport.prompt("olá", "r1")

      expect(child.stdin.writes).toHaveLength(1)

      const sent = JSON.parse(child.stdin.writes[0])
      expect(sent.v).toBe(1)
      expect(sent.type).toBe("prompt_submit")
      expect(sent.correlationId).toBe("r1")
      expect(sent.payload).toEqual({ text: "olá", sessionId: null })
    } finally {
      if (previous === undefined) delete process.env.PI_BACKEND_DLL
      else process.env.PI_BACKEND_DLL = previous
    }
  })
})
