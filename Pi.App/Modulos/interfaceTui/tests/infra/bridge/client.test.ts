// Testes do adaptador NDJSON sem iniciar o dotnet: usamos a porta de handlers
// e o buffer de leitura diretamente, com um child fictício para as saídas.
import { describe, expect, it } from "bun:test"
import { BridgeClient, type BridgeHandlers } from "../../../src/infra/bridge/client"
import type { DebugMonitor } from "../../../src/infra/debug/monitor"
import type { BridgeOutput } from "../../../src/infra/bridge/protocol"

function montar() {
  const eventos: BridgeOutput[] = []
  const linhas: string[] = []
  const handlers: BridgeHandlers = {
    onEvent: evento => eventos.push(evento),
    onExit: () => {}
  }
  const monitor = {
    enabled: true,
    logDir: "teste",
    log: () => {},
    stderr: () => {},
    ndjson: (linha: string) => linhas.push(linha),
    build: () => {},
    path: () => "teste"
  } as unknown as DebugMonitor
  const client = new BridgeClient(handlers, monitor)
  const ler = (chunk: string) =>
    (client as unknown as { readOutput(chunk: string): void }).readOutput(chunk)

  return { client, ler, eventos, linhas }
}

const ready = {
  type: "ready",
  model: "m",
  agent: "a",
  session: "s",
  cwd: "c",
  branch: "b"
} as const

describe("BridgeClient", () => {
  it("bufferiza linhas parciais até o newline", () => {
    const { ler, eventos, linhas } = montar()
    const json = JSON.stringify(ready)

    ler(json.slice(0, 15))
    expect(eventos).toHaveLength(0)
    expect(linhas).toHaveLength(0)

    ler(json.slice(15) + "\n")

    expect(eventos).toEqual([ready])
    expect(linhas).toEqual([json])
  })

  it("lê várias linhas de uma vez, ignora vazias e aceita CRLF", () => {
    const { ler, eventos, linhas } = montar()
    const json = JSON.stringify(ready)

    ler(`\n${json}\r\n${json}\n`)

    expect(eventos).toEqual([ready, ready])
    expect(linhas).toEqual([json, json])
  })

  it("linha inválida vira evento de erro sem derrubar o parser", () => {
    const { ler, eventos } = montar()

    ler("nao-e-json\n")

    expect(eventos).toHaveLength(1)
    expect(eventos[0].type).toBe("error")
    expect((eventos[0] as { message: string }).message).toContain("Linha inválida")
  })

  it("prompt, cancel e clear viram NDJSON de entrada", () => {
    const { client } = montar()
    const escritas: string[] = []
    ;(client as unknown as { child: unknown }).child = {
      stdin: { write: (texto: string) => escritas.push(texto) },
      exitCode: null
    }

    client.prompt("r1", "oi")
    client.cancel("r1")
    client.cancel()
    client.clear()

    expect(escritas).toEqual([
      '{"type":"prompt","requestId":"r1","text":"oi"}\n',
      '{"type":"cancel","requestId":"r1"}\n',
      '{"type":"cancel"}\n',
      '{"type":"clear"}\n'
    ])
  })
})
