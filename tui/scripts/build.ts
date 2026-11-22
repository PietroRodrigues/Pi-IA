import solidPlugin from "@opentui/solid/bun-plugin"

const result = await Bun.build({
  entrypoints: ["src/main.tsx"],
  target: "bun",
  outdir: "dist",
  plugins: [solidPlugin],
  // O core escolhe o binário nativo em runtime. Não devemos embutir
  // pacotes opcionais de outras plataformas no bundle do Windows.
  external: [
    "@opentui/core-linux-x64",
    "@opentui/core-linux-x64-musl",
    "@opentui/core-linux-arm64",
    "@opentui/core-linux-arm64-musl",
    "@opentui/core-darwin-x64",
    "@opentui/core-darwin-arm64",
    "@opentui/core-win32-x64",
    "@opentui/core-win32-arm64"
  ]
})

if (!result.success) {
  for (const log of result.logs) console.error(log)
  process.exit(1)
}

console.log(`TUI compilada: ${result.outputs.map(output => output.path).join(", ")}`)
