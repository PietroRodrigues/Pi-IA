import { render } from "@opentui/solid"
import { App } from "./components/App"
import { theme } from "./theme"

await render(() => <App />, {
  screenMode: "alternate-screen",
  backgroundColor: theme.background,
  exitOnCtrlC: false,
  // O App coordena o encerramento; listeners próprios do renderer engoliriam
  // SIGINT/SIGTERM sem chamar process.exit(0).
  exitSignals: [],
  targetFps: 30,
  useMouse: true
})
