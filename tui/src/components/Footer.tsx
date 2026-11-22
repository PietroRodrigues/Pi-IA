import { theme } from "../theme"
import type { ChatState } from "../state"

export function Footer(props: { state: ChatState }) {
  const stats = () => props.state.stats()

  return (
    <box
      style={{
        width: "100%",
        height: 1,
        flexDirection: "row",
        justifyContent: "space-between",
        backgroundColor: theme.panel,
        paddingLeft: 1,
        paddingRight: 1
      }}
    >
      <text content={`Pi · ${props.state.status()}`} fg={theme.text} />
      <text
        content={`Contexto: ${stats().percent.toFixed(0)}% · Ctrl+P foco · Ctrl+L limpar · Ctrl+C sair`}
        fg={theme.muted}
      />
    </box>
  )
}
