// Barra inferior: mostra o estado atual e os atalhos que cabem na linha.
import { theme } from "./theme"
import type { ChatModel } from "../model/ChatModel"

export function Footer(props: { model: ChatModel }) {
  const stats = () => props.model.stats()

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
      <text content={`Pi · ${props.model.status()}`} fg={theme.text} />
      <text
        content={`Contexto: ${stats().percent.toFixed(0)}% · Ctrl+P foco · Ctrl+L limpar · Ctrl+C sair`}
        fg={theme.muted}
      />
    </box>
  )
}
