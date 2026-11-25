// Cópia best-effort: OSC 52 primeiro e, no Windows, o comando `clip` como
// fallback. Nenhuma falha pode derrubar a TUI durante um Ctrl+C.
import { spawn } from "node:child_process"

export interface ClipboardDeps {
  readonly platform: () => string
  readonly osc52: (text: string) => boolean
  readonly nativeWrite?: (text: string) => Promise<boolean>
}

function windowsClip(text: string): Promise<boolean> {
  return new Promise(resolve => {
    try {
      const child = spawn("clip", [], {
        windowsHide: true,
        stdio: ["pipe", "ignore", "ignore"]
      })
      let settled = false
      const finish = (ok: boolean) => {
        if (settled) return
        settled = true
        clearTimeout(timer)
        resolve(ok)
      }

      const timer = setTimeout(() => {
        child.kill()
        finish(false)
      }, 800)
      child.on("error", () => finish(false))
      child.on("close", code => finish(code === 0))
      child.stdin.on("error", () => finish(false))
      child.stdin.end(text)
    } catch {
      resolve(false)
    }
  })
}

export async function copyTextToClipboard(
  text: string,
  deps: ClipboardDeps
): Promise<boolean> {
  if (text.trim().length === 0) return false

  try {
    if (deps.osc52(text)) return true
  } catch {
    // Terminal sem suporte a OSC 52: tenta o backend nativo abaixo.
  }

  const native = deps.nativeWrite ?? (deps.platform() === "win32" ? windowsClip : undefined)
  if (!native) return false

  try {
    return await native(text)
  } catch {
    return false
  }
}
