// Acompanha os arquivos de debug no terminal. Mostra o fim de cada um ao abrir
// e, depois, só o que for acrescentado.
import { existsSync, readFileSync, watchFile, unwatchFile } from "node:fs"
import { join } from "node:path"
import { LOG_FILES, resolveLogDir } from "../src/infra/debug/monitor"

const logDir = resolveLogDir()
const entries = Object.entries(LOG_FILES) as Array<[keyof typeof LOG_FILES, string]>
const offsets = new Map<string, number>()

function printNew(name: string, path: string, tailOnly: boolean): void {
  if (!existsSync(path)) return

  const content = readFileSync(path, "utf8")
  let offset = offsets.get(path) ?? 0

  if (tailOnly && offset === 0) {
    offset = Math.max(0, content.length - 4000)
    process.stdout.write(`\n=== ${name} (últimas linhas) ===\n`)
    process.stdout.write(content.slice(offset))
    offsets.set(path, content.length)
    return
  }

  if (content.length <= offset) {
    // Arquivo foi truncado/rotacionado.
    offset = 0
  }

  if (content.length > offset) {
    process.stdout.write(content.slice(offset))
    offsets.set(path, content.length)
  }
}

process.stdout.write(`Monitorando logs em ${logDir}\nCtrl+C para sair.\n`)

for (const [name, file] of entries) {
  const path = join(logDir, file)
  printNew(name, path, true)

  watchFile(path, { interval: 300 }, () => printNew(name, path, false))
}

process.on("SIGINT", () => {
  for (const [, file] of entries) unwatchFile(join(logDir, file))
  process.exit(0)
})
