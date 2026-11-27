// Diagnósticos da TUI em arquivo. Nunca escreve no stdout: ali só pode passar
// o que o OpenTUI desenha, e o backend usa o próprio stdout para NDJSON.
import { appendFileSync, existsSync, mkdirSync } from "node:fs"
import { join } from "node:path"
import { COMMANDS, ENV, GLYPHS, PATHS, TEXT } from "../constants"

export type LogName = keyof typeof PATHS.logFiles

export function findRepoRoot(): string {
  const candidates = [import.meta.dir, process.cwd()]

  for (const initial of candidates) {
    let directory = initial

    while (true) {
      if (
        existsSync(join(directory, PATHS.globalJson)) &&
        existsSync(join(directory, PATHS.solution)) &&
        existsSync(join(directory, ...PATHS.tuiPackage))
      ) {
        return directory
      }

      const parent = join(directory, PATHS.parentDirectory)
      if (parent === directory) break
      directory = parent
    }
  }

  return candidates[0]
}

export function debugEnabled(): boolean {
  const value = process.env[ENV.debug]
  return value === ENV.debugEnabledFlag || value?.toLowerCase() === ENV.debugTrueText
}

export function resolveLogDir(): string {
  return process.env[ENV.debugLogDir] ?? join(findRepoRoot(), ...PATHS.debugLogDir)
}

export class FileLogger {
  readonly enabled: boolean
  readonly logDir: string

  constructor(enabled = debugEnabled(), logDir = resolveLogDir()) {
    this.enabled = enabled
    this.logDir = logDir

    if (!enabled) return

    mkdirSync(logDir, { recursive: true })
    this.log(TEXT.logs.sessionStarted, { pid: process.pid, cwd: process.cwd() })

    process.on(COMMANDS.nodeEvents.uncaughtException, error => {
      this.log(TEXT.logs.uncaughtException, { message: error.message, stack: error.stack })
    })
    process.on(COMMANDS.nodeEvents.unhandledRejection, reason => {
      this.log(TEXT.logs.unhandledRejection, {
        reason: String(reason),
        stack: reason instanceof Error ? reason.stack : undefined
      })
    })
  }

  path(name: LogName): string {
    return join(this.logDir, PATHS.logFiles[name])
  }

  log(message: string, extra?: Record<string, unknown>): void {
    if (!this.enabled) return
    const details =
      extra && Object.keys(extra).length > 0
        ? `${GLYPHS.space}${JSON.stringify(extra)}`
        : TEXT.common.empty
    this.append(
      PATHS.logKeys.tui,
      `${TEXT.logs.timestampOpen}${new Date().toISOString()}${TEXT.logs.timestampClose}` +
        `${message}${details}${TEXT.common.newline}`
    )
  }

  stderr(chunk: string): void {
    if (!this.enabled) return
    this.append(PATHS.logKeys.tui, `${TEXT.logs.stderrPrefix}${GLYPHS.space}${chunk}`)
  }

  ndjson(line: string): void {
    if (!this.enabled) return
    this.append(PATHS.logKeys.ndjson, `${line}${TEXT.common.newline}`)
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
