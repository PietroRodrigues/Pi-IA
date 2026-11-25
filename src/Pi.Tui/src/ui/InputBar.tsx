// Campo de digitação com linha de contexto interna: agente, modelo e status.
// Envia no Enter e fica inerte enquanto o turno está ativo.
import type { InputRenderable } from "@opentui/core"
import { createEffect, For, Show } from "solid-js"
import type { SessionStore } from "../state/sessionStore"
import { inputContextSegments } from "./chatIndicators"
import {
  colors as theme,
  INPUT_BAR_GLYPH,
  INPUT_LAYOUT,
  inputBlockHeight
} from "./theme"

const colors = {
  panel: "#141414",
  panelAlt: "#1E1E1E",
  text: "#E5E5E5",
  muted: "#A3A3A3"
} as const

interface InputBarProps {
  store: SessionStore
  onSubmit: (text: string) => void
  onValueChange?: (value: string) => void
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

  const segments = () => {
    const session = props.store.session()
    return inputContextSegments(
      session.agent,
      session.model,
      props.store.status(),
      props.store.busy()
    )
  }

  // Uma linha de glifo por linha do bloco: a barra vai do topo à base sem recuo.
  const barLines = () =>
    Array.from({ length: inputBlockHeight() }, (_, index) => index)

  return (
    <box
      style={{
        // "auto" deixa o flex do pai descontar as margens: com 100% o bloco
        // estourava à direita e a margem direita ficava invisível.
        width: "auto",
        height: inputBlockHeight(),
        flexDirection: "row",
        backgroundColor: props.store.busy() ? colors.panelAlt : colors.panel,
        marginTop: INPUT_LAYOUT.marginTop,
        marginLeft: INPUT_LAYOUT.marginX,
        marginRight: INPUT_LAYOUT.marginRight,
        marginBottom: INPUT_LAYOUT.marginBottom,
        paddingLeft: 0,
        paddingRight: 1,
        paddingTop: 0,
        paddingBottom: 0
      }}
    >
      <box style={{ width: 1, flexDirection: "column", flexGrow: 1 }}>
        <For each={barLines()}>
          {() => <text content={INPUT_BAR_GLYPH} fg={theme.accent} />}
        </For>
      </box>
      <box style={{ width: 1 }} />
      <box
        style={{
          flexGrow: 1,
          minWidth: 0,
          flexDirection: "column",
          paddingTop: INPUT_LAYOUT.paddingTop,
          paddingBottom: INPUT_LAYOUT.paddingBottom,
          paddingRight: 1
        }}
      >
        <box style={{ width: "100%", height: 1, flexDirection: "row" }}>
          <input
            ref={renderable => {
              inputRef = renderable
              inputRef.placeholder = placeholder(props.store.busy())
              props.onRef?.(renderable)
            }}
            focused
            placeholder="Digite sua mensagem..."
            placeholderColor={colors.muted}
            style={{
              width: "100%",
              flexGrow: 1,
              backgroundColor: colors.panel,
              textColor: colors.text
            }}
            onInput={value => props.onValueChange?.(value)}
            onSubmit={value => {
              if (props.store.busy()) return

              // O chamador decide: com o menu aberto, Enter apenas navega e o valor
              // do campo é sincronizado por ele; sem menu, o texto vira prompt.
              props.onSubmit(value.trim())
            }}
          />
        </box>

        <box style={{ height: INPUT_LAYOUT.gapLines }} />

        <box style={{ width: "100%", height: 1, flexDirection: "row" }}>
          <text content={segments().agent} fg={theme.accent} />
          <text content=" · " fg={colors.muted} />
          <text content={segments().model} fg={colors.muted} />
          <Show when={segments().status.length > 0}>
            <text content={` · ${segments().status}`} fg={colors.muted} />
          </Show>
        </box>
      </box>
    </box>
  )
}
