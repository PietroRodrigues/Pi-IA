// Dados do ambiente onde o terminal foi aberto. Toda leitura é defensiva:
// sem git, sem rede ou sem privilégio a TUI continua abrindo normalmente.
import { execFile } from "node:child_process"
import { readFileSync } from "node:fs"
import { join } from "node:path"
import {
  COMMANDS,
  GLYPHS,
  LIMITS,
  PATHS,
  PROTOCOL,
  TEXT,
  TIME
} from "../constants"

export type AdminSource =
  | typeof COMMANDS.adminSources.windows
  | typeof COMMANDS.adminSources.posix
  | typeof COMMANDS.adminSources.unknown

export interface EnvironmentSnapshot {
  readonly cwd: string
  readonly branch: string
  readonly elevated: boolean
  readonly source: AdminSource
}

export interface EnvironmentDeps {
  readonly cwd: () => string
  readonly readGitHead: (path: string) => string
  readonly platform: () => string
  readonly uid: () => number | undefined
  readonly checkWindowsAdmin: (done: (elevated: boolean) => void) => void
}

export const emptyEnvironment: EnvironmentSnapshot = {
  cwd: TEXT.environment.currentDirectory,
  branch: TEXT.environment.unknownBranch,
  elevated: false,
  source: COMMANDS.adminSources.unknown
}

function safely<T>(read: () => T, fallback: T): T {
  try {
    return read()
  } catch {
    return fallback
  }
}

export function createEnvironmentDeps(): EnvironmentDeps {
  return {
    cwd: () => process.cwd(),
    readGitHead: path => readFileSync(path, PROTOCOL.io.encoding),
    platform: () => process.platform,
    uid: () =>
      typeof process.getuid === COMMANDS.apiValues.functionType
        ? process.getuid!()
        : undefined,
    checkWindowsAdmin: done => {
      try {
        execFile(
          COMMANDS.process.net,
          [COMMANDS.process.netSession],
          { timeout: TIME.environmentTimeoutMs, windowsHide: PROTOCOL.io.windowsHide },
          error => {
            done(!error)
          }
        )
      } catch {
        done(false)
      }
    }
  }
}

export function parseGitBranch(head: string | undefined): string {
  if (!head) return emptyEnvironment.branch

  const ref = head
    .match(new RegExp(TEXT.environment.headRefPattern, TEXT.environment.multilineFlag))?.[1]
    ?.trim()
  if (ref) return ref

  const detached = head.trim()
  if (detached.length > 0 && !detached.startsWith(TEXT.environment.detachedCommentPrefix)) {
    return `${TEXT.environment.detachedPrefix}${detached.slice(0, LIMITS.gitHashChars)}`
  }
  return emptyEnvironment.branch
}

export function elevatedLabel(elevated: boolean): string {
  return elevated ? TEXT.environment.adminBadge : TEXT.environment.standardUser
}

export function environmentLabel(env: EnvironmentSnapshot): string {
  const cwd = env.cwd.trim().length > 0 ? env.cwd : GLYPHS.emDash
  const branch =
    env.branch === emptyEnvironment.branch
      ? TEXT.common.empty
      : `${TEXT.environment.gitPrefix}${env.branch}`
  return `${cwd}${branch}${TEXT.common.separator}${elevatedLabel(env.elevated)}`
}

function windowsAdmin(
  deps: EnvironmentDeps,
  timeoutMs: number
): Promise<boolean> {
  return new Promise(resolve => {
    let settled = false
    const finish = (elevated: boolean) => {
      if (settled) return
      settled = true
      clearTimeout(timer)
      resolve(elevated)
    }

    const timer = setTimeout(() => finish(false), timeoutMs)
    try {
      deps.checkWindowsAdmin(finish)
    } catch {
      finish(false)
    }
  })
}

// Nunca rejeita: chamadas de ambiente são best-effort e não podem impedir a TUI.
export async function detectEnvironment(
  overrides: Partial<EnvironmentDeps> = {},
  timeoutMs: number = TIME.environmentTimeoutMs
): Promise<EnvironmentSnapshot> {
  const deps: EnvironmentDeps = { ...createEnvironmentDeps(), ...overrides }
  const cwd = safely(() => deps.cwd(), emptyEnvironment.cwd) || emptyEnvironment.cwd

  const branch = safely(
    () => parseGitBranch(deps.readGitHead(join(cwd, ...PATHS.gitHead))),
    emptyEnvironment.branch
  )

  let elevated = false
  let source: AdminSource = COMMANDS.adminSources.unknown

  try {
    const platform = safely(() => deps.platform(), TEXT.common.empty)
    if (platform === COMMANDS.platforms.windows) {
      elevated = await windowsAdmin(deps, timeoutMs)
      source = COMMANDS.adminSources.windows
    } else if (platform.length > 0) {
      const uid = safely(() => deps.uid(), undefined)
      if (uid !== undefined) {
        elevated = uid === LIMITS.zero
        source = COMMANDS.adminSources.posix
      }
    }
  } catch {
    elevated = false
    source = COMMANDS.adminSources.unknown
  }

  return { cwd, branch, elevated, source }
}
