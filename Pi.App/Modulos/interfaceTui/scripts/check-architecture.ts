// Guardrails da TUI: verifica a separação MVC sem dependências extras.
// Rode com `bun run check:arch` dentro de Pi.App/Modulos/interfaceTui.
import { existsSync, readFileSync, readdirSync } from "node:fs"
import { join } from "node:path"

const raiz = join(import.meta.dir, "..")
const src = join(raiz, "src")

const violacoes: string[] = []

function arquivos(diretorio: string): string[] {
  if (!existsSync(diretorio)) return []
  return readdirSync(diretorio, { withFileTypes: true }).flatMap(entrada => {
    const caminho = join(diretorio, entrada.name)
    return entrada.isDirectory() ? arquivos(caminho) : [caminho]
  })
}

function exige(pasta: string): void {
  if (!existsSync(join(src, pasta))) violacoes.push(`Pasta ausente: src/${pasta}`)
}

function proibe(pasta: string, trechos: string[]): void {
  for (const arquivo of arquivos(join(src, pasta))) {
    const texto = readFileSync(arquivo, "utf8")
    for (const trecho of trechos) {
      if (texto.includes(trecho)) {
        violacoes.push(`${arquivo.replace(raiz + "\\", "")} não deve conter "${trecho}"`)
      }
    }
  }
}

for (const pasta of ["model", "view", "controller", "infra/bridge", "infra/debug"]) {
  exige(pasta)
}

proibe("model", ['from "../infra', 'from "../view', 'from "../controller'])
proibe("view", ['from "../infra/', "BridgeClient"])
proibe("controller", ['from "../view/', 'from "@opentui/core"', 'from "@opentui/solid"'])

const testes = arquivos(join(raiz, "tests")).filter(arquivo => arquivo.endsWith(".test.ts"))
if (testes.length === 0) violacoes.push("Nenhum teste encontrado em tests/")

// A View principal pode usar @opentui (é a camada de renderização).
const viewPrincipal = readFileSync(join(src, "view", "App.tsx"), "utf8")
if (!viewPrincipal.includes("useRenderer") || !viewPrincipal.includes("useKeyboard")) {
  violacoes.push("view/App.tsx deve renderizar e delegar teclado ao Controller")
}

const controller = readFileSync(join(src, "controller", "AppController.ts"), "utf8")
for (const metodo of ["handleEvent", "submit", "cancelActive", "clearContext", "shutdown"]) {
  if (!controller.includes(metodo)) {
    violacoes.push(`controller/AppController.ts não contém ${metodo}`)
  }
}

const cliente = readFileSync(join(src, "infra", "bridge", "client.ts"), "utf8")
if (!cliente.includes("BodyTools.dll")) {
  violacoes.push("infra/bridge/client.ts deve apontar para BodyTools.dll")
}

const pacote = JSON.parse(readFileSync(join(raiz, "package.json"), "utf8")) as { name?: string }
if (pacote.name !== "interfaceTui") {
  violacoes.push(`package.json deve se chamar interfaceTui (atual: ${pacote.name})`)
}

if (violacoes.length > 0) {
  console.error("Arquitetura da TUI inválida:")
  for (const violacao of violacoes) console.error(` - ${violacao}`)
  process.exit(1)
}

console.log("Arquitetura da TUI OK (model / view / controller / infra)")
