// Ponto de entrada da TUI. Sem JSX aqui: a árvore alvo define index.ts.
import { render } from "@opentui/solid"
import { createComponent } from "solid-js"
import { App } from "./ui/App"

await render(() => createComponent(App, {}), {
  screenMode: "alternate-screen",
  backgroundColor: "#0A0A0A",
  // O controlador coordena o encerramento; listeners do renderer engoliriam
  // SIGINT/SIGTERM sem chamar process.exit(0).
  exitOnCtrlC: false,
  exitSignals: [],
  targetFps: 30,
  useMouse: true
})
