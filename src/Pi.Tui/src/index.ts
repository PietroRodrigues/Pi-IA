// Ponto de entrada da TUI. Sem JSX aqui: a árvore alvo define index.ts.
import { render } from "@opentui/solid"
import { createComponent } from "solid-js"
import { COLORS, LAYOUT, TIME } from "./constants"
import { App } from "./ui/App"

await render(() => createComponent(App, {}), {
  screenMode: LAYOUT.screenMode,
  backgroundColor: COLORS.background,
  // O controlador coordena o encerramento; listeners do renderer engoliriam
  // SIGINT/SIGTERM sem chamar process.exit(0).
  exitOnCtrlC: false,
  exitSignals: [],
  targetFps: TIME.targetFps,
  useMouse: true
})
