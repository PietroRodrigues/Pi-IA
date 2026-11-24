// Campo de digitação. Envia o texto quando dá Enter e ignora cliques enquanto gera.
import type { InputRenderable } from "@opentui/core"
import { createEffect, type Accessor } from "solid-js"
import { theme } from "./theme"

interface ComposerProps {
  onSubmit: (text: string) => void
  busy: Accessor<boolean>
  onRef?: (input: InputRenderable) => void
}

export function Composer(props: ComposerProps) {
  let inputRef: InputRenderable | undefined

  // O OpenTUI Solid não propaga mudanças de `placeholder` pela prop JSX,
  // então atualizamos o renderable diretamente quando o estado muda.
  createEffect(() => {
    const placeholder = props.busy() ? "Gerando… Esc para cancelar" : "Digite sua mensagem..."
    if (inputRef) inputRef.placeholder = placeholder
  })

  return (
    <box
      style={{
        width: "100%",
        height: 3,
        flexDirection: "row",
        backgroundColor: props.busy() ? theme.panelAlt : theme.panel,
        paddingLeft: 1,
        paddingRight: 1,
        paddingTop: 1,
        paddingBottom: 1
      }}
    >
      <text fg={theme.accent}>▎</text>
      <input
        ref={renderable => {
          inputRef = renderable
          inputRef.placeholder = props.busy()
            ? "Gerando… Esc para cancelar"
            : "Digite sua mensagem..."
          props.onRef?.(renderable)
        }}
        focused
        placeholder="Digite sua mensagem..."
        placeholderColor={theme.muted}
        style={{ flexGrow: 1, backgroundColor: theme.panel, textColor: theme.text }}
        onSubmit={value => {
          if (props.busy()) return

          const text = value.trim()
          if (text.length === 0) return

          props.onSubmit(text)
          if (inputRef) inputRef.value = ""
        }}
      />
    </box>
  )
}
