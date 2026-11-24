// Guarda logs da TUI e do backend em arquivo. Nunca escreve no stdout:
// ali só pode passar o que o OpenTUI desenha.
import { appendFileSync, existsSync, mkdirSync } from "node:fs"
import { join } from "node:path"

/** Arquivos de diagnóstico criados quando PI_DEBUG=1. Tudo fica fora do stdout. */
export const LOG_FILES = {
  stderr: "bodytools.stderr.log",
  ndjson: "bodytools.ndjson.log",
  tui: "tui.log",
  build: "dotnet-build.log"
} as const

export type LogName = keyof typeof LOG_FILES

export function findRepoRoot(): string {
  const candidatos = [import.meta.dir, process.cwd()]

  // A raiz também tem global.json; Pi.App/ sozinho não conta, senão os logs
  // acabariam em Pi.App/artifacts em vez de artifacts/. Subir de verdade evita
  // contar níveis fixos (a TUI agora fica em Pi.App/Modulos/interfaceTui).
  for (const inicial of candidatos) {
    let diretorio = inicial

    while (true) {
      if (
        existsSync(join(diretorio, "global.json")) &&
        existsSync(join(diretorio, "Pi.App", "Pi.App.slnx"))
      ) {
        return diretorio
      }

      const pai = join(diretorio, "..")
      if (pai === diretorio) break
      diretorio = pai
    }
  }

  return candidatos[0]
}

export function debugEnabled(): boolean {
  const value = process.env.PI_DEBUG
  return value === "1" || value?.toLowerCase() === "true"
}

export function resolveLogDir(): string {
  return process.env.PI_DEBUG_LOG_DIR ?? join(findRepoRoot(), "artifacts", "debug")
}

/**
 * Coleta diagnósticos da TUI e do backend sem escrever no stdout: a renderização
 * do OpenTUI e o protocolo NDJSON precisam ficar intactos.
 */
export class DebugMonitor {
  readonly enabled: boolean
  readonly logDir: string

  constructor(enabled = debugEnabled(), logDir = resolveLogDir()) {
    this.enabled = enabled
    this.logDir = logDir

    if (!enabled) return

    mkdirSync(logDir, { recursive: true })
    this.log("sessão iniciada", { pid: process.pid, cwd: process.cwd() })

    process.on("uncaughtException", error => {
      this.log("uncaughtException", { message: error.message, stack: error.stack })
    })
    process.on("unhandledRejection", reason => {
      this.log("unhandledRejection", {
        reason: String(reason),
        stack: reason instanceof Error ? reason.stack : undefined
      })
    })
  }

  path(name: LogName): string {
    return join(this.logDir, LOG_FILES[name])
  }

  log(message: string, extra?: Record<string, unknown>): void {
    if (!this.enabled) return

    const details = extra && Object.keys(extra).length > 0 ? ` ${JSON.stringify(extra)}` : ""
    this.append("tui", `[${new Date().toISOString()}] ${message}${details}\n`)
  }

  stderr(chunk: string): void {
    if (!this.enabled) return
    this.append("stderr", chunk)
  }

  ndjson(line: string): void {
    if (!this.enabled) return
    this.append("ndjson", `${line}\n`)
  }

  build(output: string): void {
    if (!this.enabled) return
    this.append("build", output)
  }

  private append(name: LogName, content: string): void {
    // O diagnóstico nunca pode derrubar a TUI.
    try {
      appendFileSync(this.path(name), content)
    } catch {
      // ignore
    }
  }
}

let shared: DebugMonitor | undefined

/** Instância única: evita handlers de exceção duplicados entre launcher, App e ponte. */
export function getDebugMonitor(): DebugMonitor {
  shared ??= new DebugMonitor()
  return shared
}
