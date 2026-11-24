// Modal de aprovação de ferramentas. Não rouba o foco: o Ctrl/Y/N/Esc é
// tratado pelo controlador e a resposta volta como tool_decision.
import { Show } from "solid-js"
import type { SessionStore } from "../state/sessionStore"

const colors = {
  panelAlt: "#1E1E1E",
  text: "#E5E5E5",
  muted: "#A3A3A3",
  accent: "#F59E0B",
  error: "#F87171"
} as const

const riskLabel: Record<string, string> = {
  ReadOnly: "somente leitura",
  Write: "escrita",
  Shell: "comando"
}

export function ApprovalModal(props: { store: SessionStore }) {
  return (
    <Show when={props.store.pendingToolRequest()}>
      {request => (
        <box
          style={{
            width: "100%",
            height: 6,
            flexDirection: "column",
            backgroundColor: colors.panelAlt,
            paddingLeft: 1,
            paddingRight: 1,
            paddingTop: 1
          }}
        >
          <text content="Permitir esta ferramenta?" fg={colors.accent} />
          <text content={`${request().name} · ${riskLabel[request().risk] ?? request().risk}`} fg={colors.text} />
          <text content={request().input.slice(0, 160)} fg={colors.muted} />
          <text content="[Y] aprovar   [N/Esc] negar" fg={colors.error} />
        </box>
      )}
    </Show>
  )
}
