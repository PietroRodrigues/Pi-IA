// Barra inferior: sessão, modelo, uso da janela de contexto e atalhos.
import type { SessionStore } from "../state/sessionStore"

const colors = {
  panel: "#141414",
  text: "#E5E5E5",
  muted: "#A3A3A3",
  accent: "#F59E0B"
} as const

export function StatusBar(props: { store: SessionStore }) {
  const left = () => {
    const session = props.store.session()
    return `Pi · ${session.model} · ${props.store.status()}`
  }

  const right = () => {
    const context = props.store.context()
    return `Contexto: ${context.percent.toFixed(0)}% · Ctrl+P foco · Ctrl+L limpar · Ctrl+C sair`
  }

  return (
    <box
      style={{
        width: "100%",
        height: 1,
        flexDirection: "row",
        justifyContent: "space-between",
        backgroundColor: colors.panel,
        paddingLeft: 1,
        paddingRight: 1
      }}
    >
      <text content={left()} fg={colors.text} />
      <text content={right()} fg={colors.muted} />
    </box>
  )
}
