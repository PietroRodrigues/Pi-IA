// Gera o bundle da TUI em dist/. Binários nativos de outras plataformas ficam
// de fora: o @opentui/core escolhe o pacote correto em runtime.
import solidPlugin from "@opentui/solid/bun-plugin"

const external = [
  "@opentui/core-linux-x64",
  "@opentui/core-linux-x64-musl",
  "@opentui/core-linux-arm64",
  "@opentui/core-linux-arm64-musl",
  "@opentui/core-darwin-x64",
  "@opentui/core-darwin-arm64",
  "@opentui/core-win32-x64",
  "@opentui/core-win32-arm64"
]

const result = await Bun.build({
  entrypoints: ["src/index.ts"],
  target: "bun",
  outdir: "dist",
  plugins: [solidPlugin],
  external
})

if (!result.success) {
  for (const log of result.logs) console.error(log)
  process.exit(1)
}

console.log(`TUI compilada: ${result.outputs.map(output => output.path).join(", ")}`)
