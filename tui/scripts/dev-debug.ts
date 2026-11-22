// Launcher de debug: liga o monitor antes de carregar a TUI e compila o backend se preciso.
import { spawnSync } from "node:child_process"
import { join } from "node:path"
import { findRepoRoot, getDebugMonitor, resolveLogDir } from "../src/debug/monitor"

// O launcher precisa ligar o debug ANTES de qualquer módulo da TUI ser carregado.
process.env.PI_DEBUG = "1"
process.env.DOTNET_ENVIRONMENT ||= "Development"
process.env.PI_DEBUG_LOG_DIR ||= resolveLogDir()

const monitor = getDebugMonitor()
const root = findRepoRoot()

if (!process.env.PI_SKIP_BUILD) {
  monitor.log("compilando backend C#...")

  const result = spawnSync(
    "dotnet",
    ["build", join(root, "src", "Pi.CLI.csproj"), "--configuration", "Debug"],
    { cwd: root, encoding: "utf8", windowsHide: true }
  )

  if (result.stdout) monitor.build(result.stdout)
  if (result.stderr) monitor.build(result.stderr)

  if (result.status !== 0 || result.error) {
    monitor.log("build do backend falhou", {
      status: result.status,
      error: result.error?.message
    })
    process.stderr.write(
      `Falha ao compilar o backend C#. Detalhes em ${monitor.path("build")}\n`
    )
    process.exit(1)
  }

  monitor.log("build do backend concluído")
} else {
  monitor.log("build do backend ignorado (PI_SKIP_BUILD=1)")
}

monitor.log("iniciando TUI", { cwd: process.cwd(), logs: monitor.logDir })
await import("../src/main")
