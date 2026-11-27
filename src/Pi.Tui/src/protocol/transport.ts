// Transporte NDJSON da TUI: dono do terminal e do subprocesso Pi.Host.
// O stdout do backend é exclusivo do protocolo; todo diagnóstico vai para arquivo.
import { spawn, type ChildProcessWithoutNullStreams } from "node:child_process"
import { existsSync } from "node:fs"
import { join } from "node:path"
import {
  COMMANDS,
  ENV,
  LIMITS,
  PATHS,
  PROTOCOL,
  TEXT,
  TIME
} from "../constants"
import { getFileLogger, findRepoRoot, type FileLogger } from "../telemetry/fileLogger"
import {
  createErrorEvent,
  encodeCommand,
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
  const configured = process.env[ENV.backendDll]
  const dll = configured
    ? configured
    : join(findRepoRoot(), ...PATHS.hostDll)

  if (!existsSync(dll)) {
    throw new Error(
      `${TEXT.errors.backendNotFound}${dll}${PROTOCOL.io.newline}${TEXT.errors.buildHint}`
    )
  }

  return dll
}

export class BridgeTransport {
  private static readonly instances = new Set<BridgeTransport>()
  private static hooksInstalled = false

  private child: ChildProcessWithoutNullStreams | undefined
  private buffer: string = TEXT.common.empty
  private stderrTail: string = TEXT.common.empty
  private disposed = false

  constructor(
    private readonly handlers: BridgeHandlers,
    private readonly logger: FileLogger = getFileLogger(),
    private readonly factory: BackendProcessFactory = (dll, env) =>
      spawn(COMMANDS.process.dotnet, [dll, COMMANDS.process.bridge, COMMANDS.process.stdio], {
        stdio: [...PROTOCOL.io.stdio],
        windowsHide: PROTOCOL.io.windowsHide,
        env
      })
  ) {
    BridgeTransport.installExitHooks()
  }

  start(): void {
    if (this.child) return

    const dll = resolveBackendDll()
    const env = { ...process.env }

    if (this.logger.enabled && !env[ENV.dotnetEnvironment]) {
      env[ENV.dotnetEnvironment] = ENV.dotnetDevelopment
    }

    const child = this.factory(dll, env)
    this.child = child
    BridgeTransport.instances.add(this)
    this.logger.log(TEXT.logs.backendStarted, { dll, pid: child.pid, debug: this.logger.enabled })

    child.stdout.setEncoding(PROTOCOL.io.encoding)
    child.stderr.setEncoding(PROTOCOL.io.encoding)

    child.stdout.on(PROTOCOL.io.data, chunk => this.receive(chunk))
    child.stderr.on(PROTOCOL.io.data, chunk => {
      this.logger.stderr(chunk)
      this.stderrTail = (this.stderrTail + chunk).slice(-LIMITS.stderrTailChars)
    })

    child.on(PROTOCOL.io.error, error => {
      this.logger.log(TEXT.logs.backendStartFailure, { message: error.message })
      this.handlers.onEvent(createErrorEvent(
        PROTOCOL.errorCodes.internal,
        `${TEXT.errors.backendStartFailed}${error.message}`
      ))
    })

    child.on(PROTOCOL.io.exit, (code, signal) => {
      BridgeTransport.instances.delete(this)
      this.logger.log(TEXT.logs.backendExited, { code, signal })
      this.handlers.onExit(code, this.stderrTail)
    })
  }

  prompt(text: string, correlationId: string, sessionId?: string | null): void {
    this.send(PROTOCOL.commandTypes.promptSubmit, { text, sessionId: sessionId ?? null }, correlationId)
  }

  interrupt(correlationId: string, reason: string = PROTOCOL.cancelReasons.user): void {
    this.send(PROTOCOL.commandTypes.interruptTurn, { reason }, correlationId)
  }

  decide(toolRequestId: string, approved: boolean, correlationId: string, reason?: string): void {
    this.send(
      PROTOCOL.commandTypes.toolDecision,
      { toolRequestId, approved, reason: reason ?? null },
      correlationId
    )
  }

  switchSession(sessionId: string, correlationId: string): void {
    this.send(PROTOCOL.commandTypes.sessionSwitch, { sessionId }, correlationId)
  }

  clear(correlationId: string): void {
    this.send(PROTOCOL.commandTypes.sessionClear, { clearHistory: true }, correlationId)
  }

  dispose(): void {
    if (this.disposed) return
    this.disposed = true

    const child = this.child
    if (!child || child.exitCode !== null || child.signalCode !== null) return

    this.logger.log(TEXT.logs.backendStopping, { pid: child.pid })

    try {
      child.stdin.end()
    } catch {
      // O pipe pode já ter fechado.
    }

    try {
      child.kill(COMMANDS.signals.terminate)
    } catch {
      // O processo pode já ter saído.
    }

    const timer = setTimeout(() => {
      if (child.exitCode === null && child.signalCode === null) {
        try {
          child.kill(COMMANDS.signals.kill)
        } catch {
          // Nada mais a fazer.
        }
      }
    }, TIME.backendKillDelayMs)
    timer.unref?.()
  }

  /** Alimenta o parser de NDJSON. Público também para testes com chunk dividido. */
  receive(chunk: string): void {
    this.buffer += chunk

    while (true) {
      const newline = this.buffer.indexOf(PROTOCOL.io.newline)
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
          createErrorEvent(
            PROTOCOL.errorCodes.invalidMessage,
            `${TEXT.errors.invalidBackendLine}${message}`
          )
        )
      }
    }
  }

  private send(type: string, payload: unknown, correlationId: string): void {
    const child = this.child
    if (!child || child.exitCode !== null || this.disposed) return

    this.logger.log(TEXT.logs.sending, { type, correlationId })
    child.stdin.write(`${encodeCommand(type as never, payload, correlationId)}${PROTOCOL.io.newline}`)
  }

  private static installExitHooks(): void {
    if (BridgeTransport.hooksInstalled) return
    BridgeTransport.hooksInstalled = true

    process.on(PROTOCOL.io.exit, () => {
      for (const client of BridgeTransport.instances) client.dispose()
    })
  }
}
