import { describe, expect, it } from "bun:test"
import { TEXT } from "../../src/constants"
import {
  detectEnvironment,
  elevatedLabel,
  emptyEnvironment,
  environmentLabel,
  parseGitBranch,
  type EnvironmentDeps
} from "../../src/ui/environment"

function deps(overrides: Partial<EnvironmentDeps> = {}): EnvironmentDeps {
  return {
    cwd: () => "E:/work/pi",
    readGitHead: () => "ref: refs/heads/main\n",
    platform: () => "linux",
    uid: () => 1000,
    checkWindowsAdmin: done => done(false),
    ...overrides
  }
}

describe("ambiente do terminal", () => {
  it("extrai o branch do HEAD do git, incluindo HEAD destacado", () => {
    expect(parseGitBranch("ref: refs/heads/feature/tui\n")).toBe("feature/tui")
    expect(parseGitBranch("abc123456789\n")).toBe("HEAD@abc12345")
    expect(parseGitBranch(undefined)).toBe("—")
    expect(parseGitBranch("# sem referência\n")).toBe("—")
  })

  it("detecta root em POSIX", async () => {
    const root = await detectEnvironment(deps({ uid: () => 0 }))
    expect(root.elevated).toBe(true)
    expect(root.source).toBe("posix-uid")
    expect(root.branch).toBe("main")

    const user = await detectEnvironment(deps())
    expect(user.elevated).toBe(false)
    expect(user.source).toBe("posix-uid")
  })

  it("usa net session no Windows com resultado injetável", async () => {
    const admin = await detectEnvironment(
      deps({ platform: () => "win32", checkWindowsAdmin: done => done(true) })
    )
    expect(admin.elevated).toBe(true)
    expect(admin.source).toBe("windows-net-session")

    const standard = await detectEnvironment(
      deps({ platform: () => "win32", checkWindowsAdmin: done => done(false) })
    )
    expect(standard.elevated).toBe(false)
  })

  it("nunca trava se a checagem do Windows não responder", async () => {
    const started = Date.now()
    const result = await detectEnvironment(
      deps({ platform: () => "win32", checkWindowsAdmin: () => {} }),
      10
    )

    expect(result.elevated).toBe(false)
    expect(Date.now() - started).toBeLessThan(1000)
  })

  it("sobrevive a erros de leitura de cwd, git e privilégio", async () => {
    const result = await detectEnvironment(
      deps({
        cwd: () => {
          throw new Error("sem cwd")
        },
        readGitHead: () => {
          throw new Error("sem git")
        },
        platform: () => {
          throw new Error("sem plataforma")
        }
      })
    )

    expect(result).toEqual(emptyEnvironment)
  })

  it("formata diretório, branch e badge de administrador", () => {
    expect(elevatedLabel(true)).toBe("ADM")
    expect(elevatedLabel(false)).toBe("Usuário padrão")

    expect(environmentLabel({
      cwd: "E:/work/pi",
      branch: "main",
      elevated: true,
      source: "posix-uid"
    })).toBe("E:/work/pi · git: main · ADM")

    expect(environmentLabel({
      cwd: "/home/user",
      branch: "—",
      elevated: false,
      source: "posix-uid"
    })).toBe("/home/user · Usuário padrão")
  })

  it("expõe os atalhos exibidos no rodapé", () => {
    expect(TEXT.environment.shortcuts).toContain("Ctrl+C")
    expect(TEXT.environment.shortcuts).toContain("Ctrl+L")
    expect(TEXT.environment.shortcuts).toContain("/sair")
    expect(TEXT.environment.shortcuts).not.toContain("Ctrl+Q")
  })
})
