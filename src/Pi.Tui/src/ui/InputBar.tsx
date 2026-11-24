// Campo de digitação. Envia no Enter e fica inerte enquanto o turno está ativo.
import type { InputRenderable } from "@opentui/core"
import { createEffect } from "solid-js"
import type { SessionStore } from "../state/sessionStore"

const colors = {
  panel: "#141414",
  panelAlt: "#1E1E1E",
  text: "#E5E5E5",
  muted: "#A3A3A3",
  accent: "#F59E0B"
} as const

interface InputBarProps {
  store: SessionStore
  onSubmit: (text: string) => void
  onRef?: (input: InputRenderable) => void
}

export function InputBar(props: InputBarProps) {
  let inputRef: InputRenderable | undefined

  const placeholder = (busy: boolean) =>
    busy ? "Gerando… Esc para cancelar" : "Digite sua mensagem..."

  createEffect(() => {
    const value = placeholder(props.store.busy())
    if (inputRef) inputRef.placeholder = value
  })

  return (
    <box
      style={{
        width: "100%",
        height: 3,
        flexDirection: "row",
        backgroundColor: props.store.busy() ? colors.panelAlt : colors.panel,
        paddingLeft: 1,
        paddingRight: 1,
        paddingTop: 1,
        paddingBottom: 1
      }}
    >
      <text fg={colors.accent}>▎</text>
      <input
        ref={renderable => {
          inputRef = renderable
          inputRef.placeholder = placeholder(props.store.busy())
          props.onRef?.(renderable)
        }}
        focused
        placeholder="Digite sua mensagem..."
        placeholderColor={colors.muted}
        style={{ flexGrow: 1, backgroundColor: colors.panel, textColor: colors.text }}
        onSubmit={value => {
          if (props.store.busy()) return

          const text = value.trim()
          if (text.length === 0) return

          props.onSubmit(text)
          if (inputRef) inputRef.value = ""
        }}
      />
    </box>
  )
}
