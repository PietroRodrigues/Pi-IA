import { describe, expect, it } from "bun:test"
import { copyTextToClipboard, type ClipboardDeps } from "../../src/ui/clipboard"

function deps(overrides: Partial<ClipboardDeps> = {}): ClipboardDeps {
  return {
    platform: () => "linux",
    osc52: () => true,
    ...overrides
  }
}

describe("cópia da seleção", () => {
  it("usa OSC 52 quando disponível e não chama o fallback", async () => {
    let nativeCalled = false
    const copied = await copyTextToClipboard("geral · qwen", deps({
      osc52: text => text.includes("qwen"),
      nativeWrite: async () => {
        nativeCalled = true
        return true
      }
    }))

    expect(copied).toBe(true)
    expect(nativeCalled).toBe(false)
  })

  it("cai para o backend nativo quando OSC 52 falha", async () => {
    let written = ""
    const copied = await copyTextToClipboard("seleção", deps({
      osc52: () => false,
      nativeWrite: async text => {
        written = text
        return true
      }
    }))

    expect(copied).toBe(true)
    expect(written).toBe("seleção")
  })

  it("não lança se todos os backends falharem", async () => {
    const copied = await copyTextToClipboard("texto", deps({
      platform: () => "win32",
      osc52: () => {
        throw new Error("sem OSC 52")
      },
      nativeWrite: async () => {
        throw new Error("sem clipboard")
      }
    }))

    expect(copied).toBe(false)
  })

  it("ignora texto vazio", async () => {
    let oscCalls = 0
    const copied = await copyTextToClipboard("   ", deps({
      osc52: () => {
        oscCalls += 1
        return true
      }
    }))

    expect(copied).toBe(false)
    expect(oscCalls).toBe(0)
  })
})
