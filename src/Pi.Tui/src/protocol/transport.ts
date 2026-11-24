// Transporte NDJSON da TUI: dono do terminal e do subprocesso Pi.Host.
// O stdout do backend é exclusivo do protocolo; todo diagnóstico vai para arquivo.
import { spawn, type ChildProcessWithoutNullStreams } from "node:child_process"
import { existsSync } from "node:fs"
import { join } from "node:path"
import { getFileLogger, findRepoRoot, type FileLogger } from "../telemetry/fileLogger"
import {
  createErrorEvent,
  encodeCommand,
  ERROR_CODES,
  parseEnvelope,
  type ProtocolMessage
} from "./schema"

export interface BridgeHandlers {
  onEvent: (event: ProtocolMessage) => void
  onExit: (code: number | null, stderrTail: string) => void
}

export type BackendProcessFactory = (
  dll: string,
  env: NodeJS.ProcessEnv
) => ChildProcessWithoutNullStreams

function resolveBackendDll(): string {
  const configured = process.env.PI_BACKEND_DLL
  const dll = configured
    ? configured
    : join(findRepoRoot(), "artifacts", "bin", "Pi.Host", "debug", "Pi.Host.dll")

  if (!existsSync(dll)) {
    throw new Error(
      `Backend C# não encontrado: ${dll}\nRode: dotnet build Pi.sln --configuration Debug`
    )
  }

  return dll
}

export class BridgeTransport {
  private static readonly instances = new Set<BridgeTransport>()
  private static hooksInstalled = false

  private child: ChildProcessWithoutNullStreams | undefined
  private buffer = ""
  private stderrTail = ""
  private disposed = false

  constructor(
    private readonly handlers: BridgeHandlers,
    private readonly logger: FileLogger = getFileLogger(),
    private readonly factory: BackendProcessFactory = (dll, env) =>
      spawn("dotnet", [dll, "--bridge", "stdio"], {
        stdio: ["pipe", "pipe", "pipe"],
        windowsHide: true,
        env
      })
  ) {
    BridgeTransport.installExitHooks()
  }

  start(): void {
    if (this.child) return

    const dll = resolveBackendDll()
    const env = { ...process.env }

    if (this.logger.enabled && !env.DOTNET_ENVIRONMENT) {
      env.DOTNET_ENVIRONMENT = "Development"
    }

    const child = this.factory(dll, env)
    this.child = child
    BridgeTransport.instances.add(this)
    this.logger.log("backend iniciado", { dll, pid: child.pid, debug: this.logger.enabled })

    child.stdout.setEncoding("utf8")
    child.stderr.setEncoding("utf8")

    child.stdout.on("data", chunk => this.receive(chunk))
    child.stderr.on("data", chunk => {
      this.logger.stderr(chunk)
      this.stderrTail = (this.stderrTail + chunk).slice(-4000)
    })

    child.on("error", error => {
      this.logger.log("falha ao iniciar o backend", { message: error.message })
      this.handlers.onEvent(createErrorEvent(ERROR_CODES.internal, `Falha ao iniciar o backend: ${error.message}`))
    })

    child.on("exit", (code, signal) => {
      BridgeTransport.instances.delete(this)
      this.logger.log("backend encerrado", { code, signal })
      this.handlers.onExit(code, this.stderrTail)
    })
  }

  prompt(text: string, correlationId: string, sessionId?: string | null): void {
    this.send("prompt_submit", { text, sessionId: sessionId ?? null }, correlationId)
  }

  interrupt(correlationId: string, reason = "user_cancel"): void {
    this.send("interrupt_turn", { reason }, correlationId)
  }

  decide(toolRequestId: string, approved: boolean, correlationId: string, reason?: string): void {
    this.send("tool_decision", { toolRequestId, approved, reason: reason ?? null }, correlationId)
  }

  switchSession(sessionId: string, correlationId: string): void {
    this.send("session_switch", { sessionId }, correlationId)
  }

  clear(correlationId: string): void {
    this.send("session_clear", { clearHistory: true }, correlationId)
  }

  dispose(): void {
    if (this.disposed) return
    this.disposed = true

    const child = this.child
    if (!child || child.exitCode !== null || child.signalCode !== null) return

    this.logger.log("encerrando backend", { pid: child.pid })

    try {
      child.stdin.end()
    } catch {
      // O pipe pode já ter fechado.
    }

    try {
      child.kill("SIGTERM")
    } catch {
      // O processo pode já ter saído.
    }

    const timer = setTimeout(() => {
      if (child.exitCode === null && child.signalCode === null) {
        try {
          child.kill("SIGKILL")
        } catch {
          // Nada mais a fazer.
        }
      }
    }, 1000)
    timer.unref?.()
  }

  /** Alimenta o parser de NDJSON. Público também para testes com chunk dividido. */
  receive(chunk: string): void {
    this.buffer += chunk

    while (true) {
      const newline = this.buffer.indexOf("\n")
      if (newline < 0) break

      const line = this.buffer.slice(0, newline).trim()
      this.buffer = this.buffer.slice(newline + 1)

      if (line.length === 0) continue

      this.logger.ndjson(line)

      try {
        this.handlers.onEvent(parseEnvelope(line))
      } catch (error) {
        const message = error instanceof Error ? error.message : String(error)
        this.handlers.onEvent(
          createErrorEvent(ERROR_CODES.invalidMessage, `Linha inválida do backend: ${message}`)
        )
      }
    }
  }

  private send(type: string, payload: unknown, correlationId: string): void {
    const child = this.child
    if (!child || child.exitCode !== null || this.disposed) return

    this.logger.log("enviando", { type, correlationId })
    child.stdin.write(`${encodeCommand(type as never, payload, correlationId)}\n`)
  }

  private static installExitHooks(): void {
    if (BridgeTransport.hooksInstalled) return
    BridgeTransport.hooksInstalled = true

    process.on("exit", () => {
      for (const client of BridgeTransport.instances) client.dispose()
    })
  }
}
