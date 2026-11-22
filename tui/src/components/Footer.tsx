import { theme } from "../theme"
import type { ChatState } from "../state"

export function Footer(props: { state: ChatState }) {
  const session = () => props.state.session()
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
      <text
        content={`${session().model} · ${props.state.status()}`}
        fg={theme.text}
      />
      <text
        content={`ctx ${stats().percent.toFixed(0)}% · ^p foco · ^l limpar · ^c sair`}
        fg={theme.muted}
      />
    </box>
  )
}
