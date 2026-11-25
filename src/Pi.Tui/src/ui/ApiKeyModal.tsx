// Janela compacta de API key. O valor fica apenas no estado visual da TUI:
// Submit marca o provedor como conectado na sessão visual e fecha a janela.
import type { ApiKeyWindowState } from "./commandMenuState"
import { colors } from "./theme"

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
      borderStyle="single"
      borderColor={colors.accent}
      style={{
        width: "100%",
        height: 6,
        flexDirection: "column",
        backgroundColor: colors.background,
        paddingLeft: 1,
        paddingRight: 1,
        paddingTop: 1
      }}
    >
      <box style={{ width: "100%", height: 1, flexDirection: "row" }}>
        <text content={props.window.provider} fg={colors.accent} />
        <box style={{ flexGrow: 1 }} />
        <box style={{ width: 4, height: 1 }} onMouseDown={props.onCancel}>
          <text content="esc" fg={colors.muted} />
        </box>
      </box>

      <box style={{ width: "100%", height: 1, flexDirection: "row" }}>
        <text content="API key " fg={colors.muted} />
        <input
          value={props.window.value}
          focused
          placeholder="digite apenas para a demonstração"
          placeholderColor={colors.muted}
          style={{ flexGrow: 1, backgroundColor: colors.panelAlt, textColor: colors.text }}
          onInput={props.onType}
          onSubmit={props.onSubmit}
        />
      </box>

      <box
        style={{
          width: "100%",
          height: 1,
          flexDirection: "row",
          justifyContent: "flex-end",
          backgroundColor: colors.panelAlt
        }}
        onMouseDown={props.onSubmit}
      >
        <text content=" Submit " fg={colors.accent} />
      </box>

      <text content="Enter confirma · nada é enviado ao host" fg={colors.muted} />
    </box>
  )
}
