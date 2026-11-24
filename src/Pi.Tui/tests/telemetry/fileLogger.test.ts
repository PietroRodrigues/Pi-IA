import { describe, expect, it } from "bun:test"
import { existsSync } from "node:fs"
import { join } from "node:path"
import { debugEnabled, findRepoRoot, resolveLogDir } from "../../src/telemetry/fileLogger"

function withEnv<T>(name: string, value: string | undefined, action: () => T): T {
  const previous = process.env[name]
  try {
    if (value === undefined) delete process.env[name]
    else process.env[name] = value
    return action()
  } finally {
    if (previous === undefined) delete process.env[name]
    else process.env[name] = previous
  }
}

describe("telemetria em arquivo", () => {
  it("encontra a raiz pela solução e pelo pacote da TUI", () => {
    const root = findRepoRoot()

    expect(existsSync(join(root, "global.json"))).toBe(true)
    expect(existsSync(join(root, "Pi.sln"))).toBe(true)
    expect(existsSync(join(root, "src", "Pi.Tui", "package.json"))).toBe(true)
  })

  it("interpreta PI_DEBUG", () => {
    expect(withEnv("PI_DEBUG", "1", debugEnabled)).toBe(true)
    expect(withEnv("PI_DEBUG", "TRUE", debugEnabled)).toBe(true)
    expect(withEnv("PI_DEBUG", "0", debugEnabled)).toBe(false)
    expect(withEnv("PI_DEBUG", undefined, debugEnabled)).toBe(false)
  })

  it("respeita PI_DEBUG_LOG_DIR", () => {
    const custom = withEnv("PI_DEBUG_LOG_DIR", "C:/temp/pi-logs", resolveLogDir)
    expect(custom).toBe("C:/temp/pi-logs")
  })
})
