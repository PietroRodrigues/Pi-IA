// Diagnósticos da TUI em arquivo. Nunca escreve no stdout: ali só pode passar
// o que o OpenTUI desenha, e o backend usa o próprio stdout para NDJSON.
import { appendFileSync, existsSync, mkdirSync } from "node:fs"
import { join } from "node:path"

export const LOG_FILES = {
  host: "pi-host.log",
  tui: "pi-tui.log",
  ndjson: "ndjson-trace.log"
} as const

export type LogName = keyof typeof LOG_FILES

export function findRepoRoot(): string {
  const candidates = [import.meta.dir, process.cwd()]

  for (const initial of candidates) {
    let directory = initial

    while (true) {
      if (
        existsSync(join(directory, "global.json")) &&
        existsSync(join(directory, "Pi.sln")) &&
        existsSync(join(directory, "src", "Pi.Tui", "package.json"))
      ) {
        return directory
      }

      const parent = join(directory, "..")
      if (parent === directory) break
      directory = parent
    }
  }

  return candidates[0]
}

export function debugEnabled(): boolean {
  const value = process.env.PI_DEBUG
  return value === "1" || value?.toLowerCase() === "true"
}

export function resolveLogDir(): string {
  return process.env.PI_DEBUG_LOG_DIR ?? join(findRepoRoot(), "artifacts", "debug")
}

export class FileLogger {
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
    this.append("tui", `[backend stderr] ${chunk}`)
  }

  ndjson(line: string): void {
    if (!this.enabled) return
    this.append("ndjson", `${line}\n`)
  }

  private append(name: LogName, content: string): void {
    try {
      appendFileSync(this.path(name), content)
    } catch {
      // O diagnóstico nunca pode derrubar a TUI.
    }
  }
}

let shared: FileLogger | undefined

export function getFileLogger(): FileLogger {
  shared ??= new FileLogger()
  return shared
}
