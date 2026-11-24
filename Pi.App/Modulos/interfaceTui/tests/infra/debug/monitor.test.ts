// Testes da infra de debug: descoberta da raiz, variáveis de ambiente e
// arquivos de log. Nenhum log é escrito fora de um diretório temporário.
import { describe, expect, it } from "bun:test"
import { existsSync, mkdtempSync, readFileSync, rmSync } from "node:fs"
import { tmpdir } from "node:os"
import { join } from "node:path"
import {
  DebugMonitor,
  debugEnabled,
  findRepoRoot,
  LOG_FILES,
  resolveLogDir
} from "../../../src/infra/debug/monitor"

function comEnv<T>(nome: string, valor: string | undefined, acao: () => T): T {
  const anterior = process.env[nome]
  try {
    if (valor === undefined) delete process.env[nome]
    else process.env[nome] = valor
    return acao()
  } finally {
    if (anterior === undefined) delete process.env[nome]
    else process.env[nome] = anterior
  }
}

describe("monitor de debug", () => {
  it("findRepoRoot retorna a raiz do repositório", () => {
    const raiz = findRepoRoot()

    expect(existsSync(join(raiz, "global.json"))).toBe(true)
    expect(existsSync(join(raiz, "Pi.App", "Pi.App.slnx"))).toBe(true)
    expect(existsSync(join(raiz, "Pi.App", "Modulos", "interfaceTui", "package.json"))).toBe(true)
    expect(raiz.replace(/\\/g, "/")).not.toContain("/Pi.App/Modulos/interfaceTui")
  })

  it("debugEnabled aceita 1/true e rejeita valores outros", () => {
    expect(comEnv("PI_DEBUG", undefined, debugEnabled)).toBe(false)
    expect(comEnv("PI_DEBUG", "1", debugEnabled)).toBe(true)
    expect(comEnv("PI_DEBUG", "TRUE", debugEnabled)).toBe(true)
    expect(comEnv("PI_DEBUG", "0", debugEnabled)).toBe(false)
  })

  it("resolveLogDir respeita PI_DEBUG_LOG_DIR e o fallback da raiz", () => {
    expect(comEnv("PI_DEBUG_LOG_DIR", "C:/logs-pi", resolveLogDir)).toBe("C:/logs-pi")
    expect(comEnv("PI_DEBUG_LOG_DIR", undefined, resolveLogDir)).toBe(
      join(findRepoRoot(), "artifacts", "debug")
    )
  })

  it("os arquivos de log usam os nomes atuais do backend", () => {
    expect(LOG_FILES).toEqual({
      stderr: "bodytools.stderr.log",
      ndjson: "bodytools.ndjson.log",
      tui: "tui.log",
      build: "dotnet-build.log"
    })
  })

  it("DebugMonitor grava cada canal no seu arquivo", () => {
    const diretorio = mkdtempSync(join(tmpdir(), "pi-tui-"))
    try {
      const monitor = new DebugMonitor(true, diretorio)

      expect(monitor.enabled).toBe(true)
      expect(monitor.logDir).toBe(diretorio)
      expect(monitor.path("tui")).toBe(join(diretorio, LOG_FILES.tui))

      monitor.log("mensagem de teste", { chave: 42 })
      monitor.stderr("linha do stderr\n")
      monitor.ndjson('{"type":"ready"}')
      monitor.build("build ok\n")

      expect(readFileSync(join(diretorio, LOG_FILES.tui), "utf8")).toContain("mensagem de teste")
      expect(readFileSync(join(diretorio, LOG_FILES.tui), "utf8")).toContain('"chave":42')
      expect(readFileSync(join(diretorio, LOG_FILES.stderr), "utf8")).toContain("linha do stderr")
      expect(readFileSync(join(diretorio, LOG_FILES.ndjson), "utf8")).toContain('{"type":"ready"}')
      expect(readFileSync(join(diretorio, LOG_FILES.build), "utf8")).toContain("build ok")
    } finally {
      rmSync(diretorio, { recursive: true, force: true })
    }
  })

  it("DebugMonitor desligado não cria arquivos", () => {
    const diretorio = mkdtempSync(join(tmpdir(), "pi-tui-"))
    try {
      const monitor = new DebugMonitor(false, diretorio)
      monitor.log("ignorado")
      monitor.stderr("ignorado")

      expect(monitor.enabled).toBe(false)
      expect(existsSync(join(diretorio, LOG_FILES.tui))).toBe(false)
      expect(existsSync(join(diretorio, LOG_FILES.stderr))).toBe(false)
    } finally {
      rmSync(diretorio, { recursive: true, force: true })
    }
  })
})
