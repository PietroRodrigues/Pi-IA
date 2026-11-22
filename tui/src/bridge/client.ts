// Vive no processo da TUI: inicia o dotnet, troca NDJSON e garante que ele morra junto.
import { spawn, type ChildProcessWithoutNullStreams } from "node:child_process"
import { existsSync } from "node:fs"
import { join } from "node:path"
import { getDebugMonitor, type DebugMonitor } from "../debug/monitor"
import type { BridgeInput, BridgeOutput } from "./protocol"

export interface BridgeHandlers {
  onEvent: (event: BridgeOutput) => void
  onExit: (code: number | null, stderrTail: string) => void
}

function resolveBackendDll(): string {
  const candidates = [
    // Arquivo-fonte: tui/src/bridge -> raiz do repositório.
    join(import.meta.dir, "..", "..", "..", "artifacts", "bin", "Pi.CLI", "debug", "Pi.CLI.dll"),
    // Bundle: tui/dist -> raiz do repositório.
    join(import.meta.dir, "..", "..", "artifacts", "bin", "Pi.CLI", "debug", "Pi.CLI.dll"),
    // Executado a partir da raiz ou de tui/.
    join(process.cwd(), "artifacts", "bin", "Pi.CLI", "debug", "Pi.CLI.dll"),
    join(process.cwd(), "..", "artifacts", "bin", "Pi.CLI", "debug", "Pi.CLI.dll")
  ]

  const dll = candidates.find(existsSync)
  if (!dll) {
    throw new Error(
      "Backend C# não encontrado. Rode: dotnet build src/Pi.CLI.csproj"
    )
  }

  return dll
}

export class BridgeClient {
  private static readonly instances = new Set<BridgeClient>()
  private static hooksInstalled = false

  private child: ChildProcessWithoutNullStreams | undefined
  // Um evento pode chegar quebrado em dois chunks; guardamos até fechar a linha.
  private buffer = ""
  private stderrTail = ""
  private disposed = false

  constructor(
    private readonly handlers: BridgeHandlers,
    private readonly monitor: DebugMonitor = getDebugMonitor()
  ) {
    BridgeClient.installExitHooks()
  }

  start(): void {
    if (this.child) return

    const dll = resolveBackendDll()
    const env = { ...process.env }

    if (this.monitor.enabled && !env.DOTNET_ENVIRONMENT) {
      // Faz o appsettings.Development.json elevar o nível de log (Pi: Debug).
      // Tudo continua indo para stderr, nunca para o stdout NDJSON.
      env.DOTNET_ENVIRONMENT = "Development"
    }

    this.child = spawn("dotnet", [dll, "--bridge", "stdio"], {
      stdio: ["pipe", "pipe", "pipe"],
      windowsHide: true,
      env
    })

    BridgeClient.instances.add(this)
    this.monitor.log("backend iniciado", { dll, pid: this.child.pid, debug: this.monitor.enabled })

    this.child.stdout.setEncoding("utf8")
    this.child.stderr.setEncoding("utf8")

    this.child.stdout.on("data", chunk => this.readOutput(chunk))
    this.child.stderr.on("data", chunk => {
      this.monitor.stderr(chunk)
      this.stderrTail = (this.stderrTail + chunk).slice(-4000)
    })

    this.child.on("error", error => {
      this.monitor.log("falha ao iniciar o backend", { message: error.message })
      this.handlers.onEvent({
        type: "error",
        requestId: "",
        message: `Falha ao iniciar o backend: ${error.message}`
      })
    })

    this.child.on("exit", (code, signal) => {
      BridgeClient.instances.delete(this)
      this.monitor.log("backend encerrado", { code, signal })
      this.handlers.onExit(code, this.stderrTail)
    })
  }

  prompt(requestId: string, text: string): void {
    this.send({ type: "prompt", requestId, text })
  }

  cancel(requestId?: string): void {
    this.send({ type: "cancel", requestId })
  }

  clear(): void {
    this.send({ type: "clear" })
  }

  dispose(): void {
    if (this.disposed) return
    this.disposed = true

    const child = this.child
    if (!child || child.exitCode !== null || child.signalCode !== null) return

    this.monitor.log("encerrando backend", { pid: child.pid })

    try {
      child.stdin.end()
    } catch {
      // O processo pode já ter fechado o pipe.
    }

    try {
      child.kill("SIGTERM")
    } catch {
      // O processo pode já ter saído.
    }

    const timer = setTimeout(() => {
      if (child.exitCode === null && child.signalCode === null) {
        try {
          // Último recurso: o backend teve 1s para sair depois do SIGTERM.
          child.kill("SIGKILL")
        } catch {
          // Nada mais a fazer.
        }
      }
    }, 1000)
    timer.unref?.()
  }

  private send(message: BridgeInput): void {
    const child = this.child
    if (!child || child.exitCode !== null || this.disposed) return

    this.monitor.log("enviando", { input: message.type, requestId: "requestId" in message ? message.requestId : undefined })
    child.stdin.write(`${JSON.stringify(message)}\n`)
  }

  private readOutput(chunk: string): void {
    this.buffer += chunk

    while (true) {
      const newline = this.buffer.indexOf("\n")
      if (newline < 0) break

      const line = this.buffer.slice(0, newline).trim()
      this.buffer = this.buffer.slice(newline + 1)

      if (line.length === 0) continue

      this.monitor.ndjson(line)

      try {
        this.handlers.onEvent(JSON.parse(line) as BridgeOutput)
      } catch {
        this.handlers.onEvent({
          type: "error",
          requestId: "",
          message: `Linha inválida recebida do backend: ${line.slice(0, 200)}`
        })
      }
    }
  }

  private static installExitHooks(): void {
    if (BridgeClient.hooksInstalled) return
    BridgeClient.hooksInstalled = true

    // Rede de segurança: qualquer saída precisa levar o subprocesso junto.
    // A saída normal é coordenada pelo App, que também destrói o renderer
    // antes de chamar process.exit(0).
    process.on("exit", () => {
      for (const client of BridgeClient.instances) client.dispose()
    })
  }
}
