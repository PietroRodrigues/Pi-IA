// Campo de digitação com linha de contexto interna: agente, modelo e status.
// Envia no Enter e fica inerte enquanto o turno está ativo.
import type { InputRenderable } from "@opentui/core"
import { createEffect, For, Show } from "solid-js"
import { COLORS, COMMANDS, GLYPHS, LAYOUT, TEXT } from "../constants"
import type { SessionStore } from "../state/sessionStore"
import { inputContextSegments } from "./chatIndicators"
import { inputBlockHeight } from "./theme"

interface InputBarProps {
  store: SessionStore
  onSubmit: (text: string) => void
  onValueChange?: (value: string) => void
  onRef?: (input: InputRenderable) => void
}

export function InputBar(props: InputBarProps) {
  let inputRef: InputRenderable | undefined

  const placeholder = (busy: boolean) =>
    busy ? TEXT.input.placeholderBusy : TEXT.input.placeholderIdle

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
        width: LAYOUT.autoWidth,
        height: inputBlockHeight(),
        flexDirection: COMMANDS.apiValues.row,
        backgroundColor: props.store.busy() ? COLORS.panelAlt : COLORS.panel,
        marginTop: LAYOUT.input.marginTop,
        marginLeft: LAYOUT.input.marginX,
        marginRight: LAYOUT.input.marginRight,
        marginBottom: LAYOUT.input.marginBottom,
        paddingLeft: LAYOUT.zero,
        paddingRight: LAYOUT.input.paddingRight,
        paddingTop: LAYOUT.zero,
        paddingBottom: LAYOUT.zero
      }}
    >
      <box style={{ width: LAYOUT.columnWidth, flexDirection: COMMANDS.apiValues.column, flexGrow: LAYOUT.one }}>
        <For each={barLines()}>
          {() => <text content={GLYPHS.inputBar} fg={COLORS.accent} />}
        </For>
      </box>
      <box style={{ width: LAYOUT.gutterWidth }} />
      <box
        style={{
          flexGrow: LAYOUT.one,
          minWidth: LAYOUT.zero,
          flexDirection: COMMANDS.apiValues.column,
          paddingTop: LAYOUT.input.paddingTop,
          paddingBottom: LAYOUT.input.paddingBottom,
          paddingRight: LAYOUT.input.paddingRight
        }}
      >
        <box style={{ width: LAYOUT.fullWidth, height: LAYOUT.lineHeight, flexDirection: COMMANDS.apiValues.row }}>
          <input
            ref={renderable => {
              inputRef = renderable
              inputRef.placeholder = placeholder(props.store.busy())
              props.onRef?.(renderable)
            }}
            focused
            placeholder={TEXT.input.placeholderIdle}
            placeholderColor={COLORS.muted}
            style={{
              width: LAYOUT.fullWidth,
              flexGrow: LAYOUT.one,
              backgroundColor: COLORS.panel,
              textColor: COLORS.text
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

        <box style={{ height: LAYOUT.input.gapLines }} />

        <box style={{ width: LAYOUT.fullWidth, height: LAYOUT.lineHeight, flexDirection: COMMANDS.apiValues.row }}>
          <text content={segments().agent} fg={COLORS.accent} />
          <text content={TEXT.common.separator} fg={COLORS.muted} />
          <text content={segments().model} fg={COLORS.muted} />
          <Show when={segments().status.length > 0}>
            <text content={`${TEXT.common.separator}${segments().status}`} fg={COLORS.muted} />
          </Show>
        </box>
      </box>
    </box>
  )
}
