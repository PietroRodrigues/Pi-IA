// Dados do ambiente onde o terminal foi aberto. Toda leitura é defensiva:
// sem git, sem rede ou sem privilégio a TUI continua abrindo normalmente.
import { execFile } from "node:child_process"
import { readFileSync } from "node:fs"
import { join } from "node:path"

export type AdminSource = "windows-net-session" | "posix-uid" | "unknown"

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
  cwd: ".",
  branch: "—",
  elevated: false,
  source: "unknown"
}

export const SHORTCUTS_LABEL =
  "Ctrl+C copiar/cancelar · /sair encerrar · Ctrl+L limpar · Ctrl+P foco"

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
    readGitHead: path => readFileSync(path, "utf8"),
    platform: () => process.platform,
    uid: () => (typeof process.getuid === "function" ? process.getuid() : undefined),
    checkWindowsAdmin: done => {
      try {
        execFile("net", ["session"], { timeout: 800, windowsHide: true }, error => {
          done(!error)
        })
      } catch {
        done(false)
      }
    }
  }
}

export function parseGitBranch(head: string | undefined): string {
  if (!head) return emptyEnvironment.branch

  const ref = head.match(/^ref:\s*refs\/heads\/(.+)$/m)?.[1]?.trim()
  if (ref) return ref

  const detached = head.trim()
  if (detached.length > 0 && !detached.startsWith("#")) {
    return `HEAD@${detached.slice(0, 8)}`
  }
  return emptyEnvironment.branch
}

export function elevatedLabel(elevated: boolean): string {
  return elevated ? "ADM" : "Usuário padrão"
}

export function environmentLabel(env: EnvironmentSnapshot): string {
  const cwd = env.cwd.trim().length > 0 ? env.cwd : "—"
  const branch = env.branch === emptyEnvironment.branch ? "" : ` · git: ${env.branch}`
  return `${cwd}${branch} · ${elevatedLabel(env.elevated)}`
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
  timeoutMs = 800
): Promise<EnvironmentSnapshot> {
  const deps: EnvironmentDeps = { ...createEnvironmentDeps(), ...overrides }
  const cwd = safely(() => deps.cwd(), emptyEnvironment.cwd) || emptyEnvironment.cwd

  const branch = safely(
    () => parseGitBranch(deps.readGitHead(join(cwd, ".git", "HEAD"))),
    emptyEnvironment.branch
  )

  let elevated = false
  let source: AdminSource = "unknown"

  try {
    const platform = safely(() => deps.platform(), "")
    if (platform === "win32") {
      elevated = await windowsAdmin(deps, timeoutMs)
      source = "windows-net-session"
    } else if (platform.length > 0) {
      const uid = safely(() => deps.uid(), undefined)
      if (uid !== undefined) {
        elevated = uid === 0
        source = "posix-uid"
      }
    }
  } catch {
    elevated = false
    source = "unknown"
  }

  return { cwd, branch, elevated, source }
}
