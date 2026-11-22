import { render } from "@opentui/solid"
import { App } from "./components/App"
import { theme } from "./theme"

await render(() => <App />, {
  screenMode: "alternate-screen",
  backgroundColor: theme.background,
  exitOnCtrlC: false,
  targetFps: 30,
  useMouse: true
})
