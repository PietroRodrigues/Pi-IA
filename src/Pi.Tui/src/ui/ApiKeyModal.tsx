// Janela compacta de API key. O valor fica apenas no estado visual da TUI:
// Submit marca o provedor como conectado na sessão visual e fecha a janela.
import { COLORS, COMMANDS, LAYOUT, TEXT } from "../constants"
import type { ApiKeyWindowState } from "./commandMenuState"

interface ApiKeyModalProps {
  readonly window: ApiKeyWindowState
  readonly onType: (value: string) => void
  readonly onSubmit: () => void
  readonly onCancel: () => void
}

export function ApiKeyModal(props: ApiKeyModalProps) {
  return (
    <box
      border
      borderStyle={COMMANDS.apiValues.single}
      borderColor={COLORS.accent}
      style={{
        width: LAYOUT.fullWidth,
        height: LAYOUT.modalHeight,
        flexDirection: COMMANDS.apiValues.column,
        backgroundColor: COLORS.background,
        paddingLeft: LAYOUT.one,
        paddingRight: LAYOUT.one,
        paddingTop: LAYOUT.one
      }}
    >
      <box style={{ width: LAYOUT.fullWidth, height: LAYOUT.lineHeight, flexDirection: COMMANDS.apiValues.row }}>
        <text content={props.window.provider} fg={COLORS.accent} />
        <box style={{ flexGrow: LAYOUT.one }} />
        <box
          style={{ width: LAYOUT.searchBoxWidth, height: LAYOUT.lineHeight }}
          onMouseDown={props.onCancel}
        >
          <text content={TEXT.apiKey.close} fg={COLORS.muted} />
        </box>
      </box>

      <box style={{ width: LAYOUT.fullWidth, height: LAYOUT.lineHeight, flexDirection: COMMANDS.apiValues.row }}>
        <text content={TEXT.apiKey.label} fg={COLORS.muted} />
        <input
          value={props.window.value}
          focused
          placeholder={TEXT.apiKey.placeholder}
          placeholderColor={COLORS.muted}
          style={{
            flexGrow: LAYOUT.one,
            backgroundColor: COLORS.panelAlt,
            textColor: COLORS.text
          }}
          onInput={props.onType}
          onSubmit={props.onSubmit}
        />
      </box>

      <box
        style={{
          width: LAYOUT.fullWidth,
          height: LAYOUT.lineHeight,
          flexDirection: COMMANDS.apiValues.row,
          justifyContent: COMMANDS.apiValues.flexEnd,
          backgroundColor: COLORS.panelAlt
        }}
        onMouseDown={props.onSubmit}
      >
        <text content={TEXT.apiKey.submit} fg={COLORS.accent} />
      </box>

      <text content={TEXT.apiKey.footer} fg={COLORS.muted} />
    </box>
  )
}
