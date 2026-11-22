import { theme } from "../theme"
import type { ChatState } from "../state"

function folderName(path: string): string {
  const parts = path.split(/[\\/]/).filter(Boolean)
  return parts.at(-1) ?? path
}

export function Sidebar(props: { state: ChatState }) {
  return (
    <box
      style={{
        width: 30,
        flexDirection: "column",
        backgroundColor: theme.panel,
        padding: 1,
        gap: 1
      }}
    >
      <text fg={theme.accent}>PI · SESSÃO</text>
      <text content={props.state.session().session} fg={theme.text} />
      <text content={props.state.session().agent} fg={theme.muted} />

      <box style={{ height: 1, backgroundColor: theme.panelAlt }} />

      <text fg={theme.muted}>CONTEXTO</text>
      <text
        content={`${props.state.stats().used} / ${props.state.stats().limit}`}
        fg={theme.text}
      />
      <text
        content={`${props.state.stats().percent.toFixed(1)}%`}
        fg={theme.accent}
      />

      <box style={{ height: 1, backgroundColor: theme.panelAlt }} />

      <text fg={theme.muted}>DIRETÓRIO</text>
      <text content={folderName(props.state.session().cwd)} fg={theme.text} />
      <text content={props.state.session().cwd} fg={theme.muted} />

      <box style={{ height: 1, backgroundColor: theme.panelAlt }} />

      <text fg={theme.muted}>BRANCH</text>
      <text content={props.state.session().branch} fg={theme.success} />
    </box>
  )
}
